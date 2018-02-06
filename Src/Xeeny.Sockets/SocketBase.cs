using Microsoft.Extensions.Logging;
using Xeeny.Sockets.Messages;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Sockets
{
    public abstract class SocketBase : ISocket
    {
        public event Action<ISocket, Message> RequestReceived;

        public event Action<IConnectionObject> StateChanged;
        ConnectionState _state;
        public ConnectionState State
        {
            get => _state;
            protected set
            {
                if (_state != value)
                {
                    _state = value;
                    Logger.LogTrace($"State changed to {value}");
                    OnStateChanged();
                }
            }
        }

        public string Id => _id;

        readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        byte _leftKeepAliveRetries = 0;
        bool _isSending = false;

        readonly MessageParser _parser;
        readonly ResponseManager _responseManager = new ResponseManager();

        readonly int _timeout;
        readonly int _receiveTimeout;
        readonly int _keepAliveInterval;
        readonly byte _keepAliveRetries;

        readonly int _sendBufferSize;
        readonly int _receiveBufferSize;

        readonly string _id;


        public readonly string ConnectionId;
        protected readonly ILogger Logger;

        public SocketBase(SocketSettings settings, ILogger logger)
        {
            _timeout = settings.Timeout.TotalMilliseconds;
            _receiveTimeout = settings.ReceiveTimeout.TotalMilliseconds;
            _keepAliveInterval = settings.KeepAliveInterval.TotalMilliseconds;
            _keepAliveRetries = settings.KeepAliveRetries;

            _sendBufferSize = settings.SendBufferSize;
            _receiveBufferSize = settings.ReceiveBufferSize;

            _parser = new MessageParser(settings.MaxMessageSize);
            _leftKeepAliveRetries = settings.KeepAliveRetries;

            _id = Guid.NewGuid().ToString();
            ConnectionId = $"ConnectionId: {_id}";

            Logger = logger;
        }

        protected abstract Task OnConnect(CancellationToken ct);
        protected abstract void OnClose(CancellationToken ct);
        protected abstract Task Send(IEnumerable<ArraySegment<byte>> segments, CancellationToken ct);
        protected abstract Task<byte[]> Receive(ArraySegment<byte> receiveBuffer, MessageParser parser, CancellationToken ct);

        public async Task Connect()
        {
            if (CanConnect())
            {
                await _lock.WaitAsync();
                try
                {
                    if (CanConnect())
                    {
                        State = ConnectionState.Connecting;
                        using (var cts = new CancellationTokenSource(_timeout))
                        using (cts.Token.Register(Close))
                        {
                            await OnConnect(CancellationToken.None);
                            State = ConnectionState.Connected;
                        }
                    }
                }
                catch(Exception ex)
                {
                    Logger.LogError(ex, $"Connection to failed");
                    Close();
                    throw;
                }
                finally
                {
                    _lock.Release();
                }
            }
        }

        public void Close()
        {
            Close(true);
        }

        async void Close(bool sendClose)
        {
            if (CanClose())
            {
                await _lock.WaitAsync();
                try
                {
                    if(CanClose())
                    {
                        State = ConnectionState.Closing;
                        if (sendClose)
                        {
                            try
                            {
                                using (var cts = new CancellationTokenSource(_timeout))
                                using (cts.Token.Register(Close))
                                {
                                    var closeMsg = _parser.GetCloseBytes();
                                    var closeSegments = GetSegments(closeMsg);
                                    await Send(closeSegments, cts.Token);
                                }
                            }
                            catch { }
                        }
                        OnClose(CancellationToken.None);
                    }
                }
                catch(Exception ex)
                {
                    Logger.LogError(ex, "Graceful close failed");
                }
                finally
                {
                    State = ConnectionState.Closed;
                    _lock.Release();
                }
            }
        }

        public void Dispose() => Close();

        bool CanConnect()
        {
            if (State == ConnectionState.Connecting || State == ConnectionState.Connected)
                return false;

            if (State != ConnectionState.None)
                throw new Exception($"Can not connect, socket is closed");

            return true;
        }

        bool CanClose()
        {
            return State < ConnectionState.Closing;
        }

        public virtual async Task Send(byte[] bytes)
        {
            await _lock.WaitAsync();
            try
            {
                using (var cts = new CancellationTokenSource(_timeout))
                using (cts.Token.Register(Close))
                {
                    _isSending = true;
                    var segments = GetSegments(bytes);
                    await Send(segments, cts.Token);
                    _leftKeepAliveRetries = _keepAliveRetries;
                }
            }
            catch(Exception ex)
            {
                _leftKeepAliveRetries--;
                throw;
            }
            finally
            {
                Logger.LogTrace($"KeepAliveRetries: {_leftKeepAliveRetries}");

                _isSending = false;
                _lock.Release();
            }
        }

        IEnumerable<ArraySegment<byte>> GetSegments(byte[] msg)
        {
            var length = msg.Length;
            if (length <= _sendBufferSize)
            {
                yield return new ArraySegment<byte>(msg);
            }
            else
            {
                var batches = (int)Math.Ceiling((double)length / _sendBufferSize);
                var index = 0;
                for (int i = 0; i < batches; i++)
                {
                    var take = i == batches - 1 ? length - index : _sendBufferSize;
                    yield return new ArraySegment<byte>(msg, index, take);
                    index += take;
                }
            }
        }

        public async void StartPing()
        {
            var pingMsg = _parser.GetPingBytes();
            while (this.State == ConnectionState.Connected)
            {
                try
                {
                    await Task.Delay(_keepAliveInterval);

                    if (_isSending)
                        continue;

                    Logger.LogTrace($"{ConnectionId} pinging, left retries: {_leftKeepAliveRetries}");
                    await Send(pingMsg);
                }
                catch(Exception ex)
                {
                    _leftKeepAliveRetries--;
                    Logger.LogTrace($"{ConnectionId} pinging failed, left retries: {_leftKeepAliveRetries}");
                    if (_leftKeepAliveRetries == 0)
                    {
                        Close(false);
                    }
                }
            }
        }

        public async void Listen()
        {
            var receiveBuffer = ArrayPool<byte>.Shared.Rent(_receiveBufferSize);

            try
            {
                var receiveSegment = new ArraySegment<byte>(receiveBuffer);
                while (this.State == ConnectionState.Connected)
                {
                    Logger.LogTrace($"{ConnectionId} receiving");
                    using (var cts = new CancellationTokenSource(_receiveTimeout))
                    using (var reg = cts.Token.Register(Close))
                    {
                        var bytes = await Receive(receiveSegment, _parser, cts.Token);

                        var msg = _parser.GetMessage(bytes);

                        switch (msg.MessageType)
                        {
                            case MessageType.Ping: break;

                            case MessageType.Close:
                                {
                                    Close(false); break;
                                }
                            case MessageType.Connect:
                                {
                                    OnRequestReceived(msg); break;
                                }
                            case MessageType.Request:
                                {
                                    OnRequestReceived(msg); break;
                                }
                            case MessageType.OneWayRequest:
                                {
                                    OnRequestReceived(msg); break;
                                }
                            case MessageType.Response:
                                {
                                    _responseManager.SetResponse(msg.Id, msg);
                                    break;
                                }
                            case MessageType.Error:
                                {
                                    _responseManager.SetResponse(msg.Id, msg);
                                    break;
                                }
                            default: throw new NotSupportedException(msg.MessageType.ToString());
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, $"{ConnectionId} stopped receiving");
                Close(true);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(receiveBuffer);
            }
        }

        protected virtual void OnStateChanged()
        {
            this.StateChanged?.Invoke(this);
        }

        protected virtual void OnRequestReceived(Message message)
        {
            this.RequestReceived?.Invoke(this, message);
        }

        public async Task SendOneWay(Message message)
        {
            LogStarted(message);

            if (message.Id != Guid.Empty || message.MessageType != MessageType.OneWayRequest)
            {
                throw new Exception($"Invalid message wrapping");
            }

            var request = _parser.GetBytes(message);
            await Send(request);

            LogEnded(message);
        }

        public async Task SendResponse(Message message)
        {
            LogStarted(message);

            if (message.Id == Guid.Empty || message.MessageType != MessageType.Response)
            {
                throw new Exception($"Invalid message wrapping");
            }

            var request = _parser.GetBytes(message);
            await Send(request);

            LogEnded(message);
        }

        public async Task SendError(Message message)
        {
            LogStarted(message);

            if (message.Id == Guid.Empty || message.MessageType != MessageType.Error)
            {
                throw new Exception($"Invalid message wrapping");
            }

            var request = _parser.GetBytes(message);
            await Send(request);

            LogEnded(message);
        }

        public async Task<Message> SendRequest(Message message)
        {
            LogStarted(message);

            if (message.Id == Guid.Empty || message.MessageType != MessageType.Request)
            {
                throw new Exception($"Message Id of message type {message.MessageType} can not be empty");
            }

            var context = _responseManager.CreateReponseContext(message.Id, _timeout);
            try
            {
                var request = _parser.GetBytes(message);

                await Send(request);

                //accept this not async as the server is always one way and never calls SendRequest
                //best async was with CancellationToken and TaskCompletionSource
                //AutoResetEvent performed better than async
                var response = context.GetResponse();

                LogEnded(message);

                return response;
            }
            catch(Exception ex)
            {
                LogError(ex, message, "Failed to send and get response");
                throw;
            }
            finally
            {
                _responseManager.RemoveResponseContext(context);
            }
        }

        void LogStarted(Message msg)
        {
            Logger.LogTrace("Started", ConnectionId, msg.Id, msg.MessageType, msg.Address, msg.Payload?.Length);
        }

        void LogEnded(Message msg)
        {
            Logger.LogTrace("Ended", ConnectionId, msg.Id, msg.MessageType, msg.Address, msg.Payload?.Length);
        }

        void LogError(Exception ex, Message msg, string message)
        {
            Logger.LogError(ex, message, ConnectionId, msg.Id, msg.MessageType, msg.Address, msg.Payload?.Length);
        }


    }
}
