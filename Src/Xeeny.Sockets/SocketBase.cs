using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Sockets.Protocol.Formatters;
using Xeeny.Sockets.Protocol.Messages;

namespace Xeeny.Sockets
{
    public abstract class SocketBase : ISocket
    {
        public string Id => _id;
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
                    Logger.LogTrace($"{_id} State changed to {value}");
                    OnStateChanged();
                }
            }
        }

        readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        byte _leftKeepAliveRetries = 0;
        bool _isSending = false;

        readonly ResponseManager _responseManager = new ResponseManager();

        readonly int _timeout;
        readonly int _receiveTimeout;
        readonly int _keepAliveInterval;
        readonly byte _keepAliveRetries;

        static readonly int _minMessageSize = Math.Max(AgreementMessage.MessageFixedSize, FragmentFormatter.ProtocolMinMessageSize);
        readonly int _maxMessageSize;
        readonly int _sendBufferSize;
        readonly int _receiveBufferSize;

        readonly string _id;

        protected readonly ILogger Logger;

        AgreementMessage? _localAgreement;
        AgreementMessage? _remoteAgreement;

        FragmentFormatter _formatter;

        public SocketBase(SocketSettings settings, ILogger logger)
        {
            var sendBufferSize = settings.SendBufferSize;
            var receiveBufferSize = settings.ReceiveBufferSize;
            var maxMessageSize = settings.MaxMessageSize;

            if(maxMessageSize < _minMessageSize)
            {
                throw new Exception($"{nameof(settings.MaxMessageSize)} can not be less than {_minMessageSize}");
            }

            if (sendBufferSize < _minMessageSize || sendBufferSize > maxMessageSize)
            {
                throw new Exception($"{nameof(settings.SendBufferSize)} must be between " +
                    $"[{_minMessageSize}, {maxMessageSize}]");
            }

            if (receiveBufferSize < _minMessageSize || receiveBufferSize > maxMessageSize)
            {
                throw new Exception($"{nameof(settings.SendBufferSize)} must be between " +
                    $"[{_minMessageSize}, {maxMessageSize}]");
            }

            _maxMessageSize = maxMessageSize;
            _sendBufferSize = sendBufferSize;
            _receiveBufferSize = receiveBufferSize;

            _timeout = settings.Timeout.TotalMilliseconds;
            _receiveTimeout = settings.ReceiveTimeout.TotalMilliseconds;
            _keepAliveInterval = settings.KeepAliveInterval.TotalMilliseconds;
            _keepAliveRetries = settings.KeepAliveRetries;

            _leftKeepAliveRetries = settings.KeepAliveRetries;

            _id = Guid.NewGuid().ToString();

            Logger = logger;
        }

        protected abstract Task OnConnect(CancellationToken ct);
        protected abstract void OnClose(CancellationToken ct);
        protected abstract Task Send(ArraySegment<byte> segment, CancellationToken ct);
        protected abstract Task<int> Receive(ArraySegment<byte> receiveBuffer, CancellationToken ct);

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
                            var ct = cts.Token;
                            await OnConnect(ct);
                            await ExchangeAgreement();
                            State = ConnectionState.Connected;
                        }
                    }
                }
                catch (Exception ex)
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

        async Task ExchangeAgreement()
        {
            if(_localAgreement == null)
            {
                using (var cts = new CancellationTokenSource(_timeout))
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(AgreementMessage.MessageFixedSize);
                    try
                    {
                        var localAgreement = new AgreementMessage(_sendBufferSize, _timeout);
                        AgreementMessage.Write(localAgreement, buffer);
                        var segment = new ArraySegment<byte>(buffer);
                        await Send(segment, cts.Token);

                        if(_remoteAgreement == null)
                        {
                            segment = new ArraySegment<byte>(buffer);
                            await Receive(segment, cts.Token);
                            var remoteAgreement = AgreementMessage.ReadMessage(segment.Array);

                            _remoteAgreement = remoteAgreement;
                        }

                        _localAgreement = localAgreement;
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
            }

            var ra = _remoteAgreement.Value;
            if(ra.FragmentSize > _receiveBufferSize)
            {
                throw new Exception($"Remote send buffer is larger than local receive buffer");
            }

            _formatter = new FragmentFormatter(_maxMessageSize, ra.FragmentSize, TimeSpan.FromMilliseconds(ra.Timeout));
        }

        public async void Listen()
        {
            var receiveBuffer = ArrayPool<byte>.Shared.Rent(_receiveBufferSize);
            try
            {
                var _receiveSegment = new ArraySegment<byte>(receiveBuffer);

                while (this.State == ConnectionState.Connected)
                {
                    Logger.LogTrace($"Connection {_id}: receiving");

                    using (var cts = new CancellationTokenSource(_receiveTimeout))
                    using (var reg = cts.Token.Register(Close))
                    {
                        var count = await Receive(_receiveSegment, cts.Token);

                        var messageType = (MessageType)_receiveSegment.Array[0];

                        switch (messageType)
                        {
                            case MessageType.Ping: break;

                            case MessageType.Close:
                                {
                                    Close(false); break;
                                }
                            case MessageType.Agreement:
                                {
                                    _remoteAgreement = AgreementMessage.ReadMessage(receiveBuffer);
                                    await ExchangeAgreement();
                                    break;
                                }
                            case MessageType.OneWayRequest:
                            case MessageType.Request:
                                {
                                    var result = _formatter.ReadMessage(receiveBuffer, count);
                                    if (result.IsComplete)
                                    {
                                        OnRequestReceived(result.Message);
                                    }
                                    break;
                                }
                            case MessageType.Response:
                            case MessageType.Error:
                                {
                                    var result = _formatter.ReadMessage(receiveBuffer, count);
                                    if (result.IsComplete)
                                    {
                                        var msg = result.Message;
                                        _responseManager.SetResponse(msg.Id, msg);
                                    }
                                    break;
                                }
                            default: throw new NotSupportedException(messageType.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{_id} stopped receiving");
                Close(true);
            }
            finally
            {
                _formatter?.Dispose();
                ArrayPool<byte>.Shared.Return(receiveBuffer);
            }
        }

        public async void StartPing()
        {
            var pingMsg = PingMessage.Bytes;
            while (this.State == ConnectionState.Connected)
            {
                try
                {
                    await Task.Delay(_keepAliveInterval);

                    if (_isSending)
                        continue;

                    Logger.LogTrace($"Connection {_id} pinging, left retries: {_leftKeepAliveRetries}");
                    await SendSegment(pingMsg);
                }
                catch (Exception ex)
                {
                    _leftKeepAliveRetries--;
                    Logger.LogTrace($"Connection {_id} pinging failed, left retries: {_leftKeepAliveRetries}");
                    if (_leftKeepAliveRetries == 0)
                    {
                        Close(false);
                    }
                }
            }
        }

        public async Task SendOneWay(Message message)
        {
            LogStarted(message);

            if (message.MessageType != MessageType.OneWayRequest)
            {
                throw new Exception($"Invalid message wrapping");
            }

            await SendMessage(message);

            LogEnded(message);
        }

        public async Task SendResponse(Message message)
        {
            LogStarted(message);

            if (message.MessageType != MessageType.Response)
            {
                throw new Exception($"Invalid message wrapping");
            }

            await SendMessage(message);

            LogEnded(message);
        }

        public async Task SendError(Message message)
        {
            LogStarted(message);

            if (message.MessageType != MessageType.Error)
            {
                throw new Exception($"Invalid message wrapping");
            }

            await SendMessage(message);

            LogEnded(message);
        }

        public async Task<Message> SendRequest(Message message)
        {
            LogStarted(message);

            if (message.MessageType != MessageType.Request)
            {
                throw new Exception($"Message Id of message type {message.MessageType} can not be empty");
            }

            var context = _responseManager.CreateReponseContext(message.Id, _timeout);
            try
            {
                await SendMessage(message);

                //accept this not async as the server is always one way and never calls SendRequest
                //best async was with CancellationToken and TaskCompletionSource
                //AutoResetEvent performed better than async
                var response = context.GetResponse();

                LogEnded(message);

                return response;
            }
            catch (Exception ex)
            {
                LogError(ex, message, "Failed to send and get response");
                throw;
            }
            finally
            {
                _responseManager.RemoveResponseContext(context);
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
                                    var closeMsg = CloseMessage.Bytes;
                                    await Send(closeMsg, cts.Token);
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

        async Task SendMessage(Message msg)
        {
            var sendBuffer = ArrayPool<byte>.Shared.Rent(_sendBufferSize);
            try
            {
                var writeResult = _formatter.WriteMessage(msg, sendBuffer, _sendBufferSize);
                while(writeResult.HasMessages)
                {
                    await SendSegment(writeResult.Message);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sendBuffer);
            }
        }

        async Task SendSegment(ArraySegment<byte> segment)
        {
            await _lock.WaitAsync();
            try
            {
                using (var cts = new CancellationTokenSource(_timeout))
                using (cts.Token.Register(Close))
                {
                    _isSending = true;
                    await Send(segment, cts.Token);
                }
            }
            finally
            {
                _isSending = false;
                _lock.Release();
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

        void LogStarted(Message msg)
        {
            Logger.LogTrace("Started", _id, msg.MessageType);
        }

        void LogEnded(Message msg)
        {
            Logger.LogTrace("Ended", _id, msg.MessageType);
        }

        void LogError(Exception ex, Message msg, string message)
        {
            Logger.LogError(ex, message, _id, msg.MessageType);
        }
    }
}
