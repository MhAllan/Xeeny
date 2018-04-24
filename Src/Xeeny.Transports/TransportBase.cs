using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports
{
    public abstract class TransportBase : ITransport
    {
        public string ConnectionId => _id;
        public string ConnectionName => _connectionName;
        public ConnectionSide ConnectionSide => _connectionSide;

        public event Action<ITransport, Message> RequestReceived;
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
                    LogTrace($"State changed to {value}");
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

        readonly string _id;
        readonly string _connectionName;
        readonly ConnectionSide _connectionSide;

        readonly ILogger _logger;

        public TransportBase(TransportSettings settings, ConnectionSide connectionSide, ILogger logger)
        {
            _timeout = settings.Timeout.TotalMilliseconds;
            _receiveTimeout = settings.ReceiveTimeout.TotalMilliseconds;
            _keepAliveInterval = settings.KeepAliveInterval.TotalMilliseconds;
            _keepAliveRetries = settings.KeepAliveRetries;

            _leftKeepAliveRetries = settings.KeepAliveRetries;

            _id = Guid.NewGuid().ToString();
            var nameFormatter = settings.ConnectionNameFormatter;
            _connectionName = nameFormatter == null ? $"Connection ({_id})" : nameFormatter(_id);
            _connectionSide = connectionSide;

            _logger = logger;
        }

        protected abstract Task OnConnect(CancellationToken ct);
        protected abstract Task OnClose(CancellationToken ct);
        protected abstract void OnKeepAlivedReceived(Message message);
        protected abstract void OnAgreementReceived(Message message);
        protected abstract Task SendMessage(Message message, CancellationToken ct);
        protected abstract Task<Message> ReceiveMessage(CancellationToken ct);

        public async Task Connect()
        {
            if (CanConnect())
            {
                await _lock.WaitAsync();
                try
                {
                    if (CanConnect())
                    {
                        LogTrace("Connecting...");
                        State = ConnectionState.Connecting;
                        using (var cts = new CancellationTokenSource(_timeout))
                        using (cts.Token.Register(Close, new CloseBehavior(false, "Connect Timeout")))
                        {
                            var ct = cts.Token;
                            await OnConnect(ct);
                            State = ConnectionState.Connected;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "Connection failed");
                    Close(new CloseBehavior(false, "Connection failed"));
                    throw;
                }
                finally
                {
                    _lock.Release();
                }
            }
        }

        public async void Listen()
        {
            try
            {
                while (this.State == ConnectionState.Connected)
                {
                    LogTrace($"Receiving...");

                    using (var cts = new CancellationTokenSource(_receiveTimeout))
                    using (cts.Token.Register(Close, new CloseBehavior(true, "Receive timeout")))
                    {
                        var message = await ReceiveMessage(cts.Token);

                        cts.Token.ThrowIfCancellationRequested();

                        LogTrace("Received message", message);

                        var messageType = message.MessageType;

                        switch (messageType)
                        {
                            case MessageType.KeepAlive:
                                {
                                    OnKeepAlivedReceived(message);
                                    break;
                                }
                            case MessageType.Close:
                                {
                                    Close(new CloseBehavior(false, "Close message received")); break;
                                }
                            case MessageType.Agreement:
                                {
                                    OnAgreementReceived(message);
                                    break;
                                }
                            case MessageType.OneWayRequest:
                            case MessageType.Request:
                                {
                                    OnRequestReceived(message);
                                    break;
                                }
                            case MessageType.Response:
                            case MessageType.Error:
                                {
                                    _responseManager.SetResponse(message.Id, message);
                                    break;
                                }
                            default: throw new NotSupportedException(messageType.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (this.State == ConnectionState.Connected)
                {
                    LogError(ex, "Stopped receiving");
                    Close(new CloseBehavior(true, $"Listenning error {ex.Message}"));
                }
                else
                {
                    LogTrace($"stopped receiving");
                }
            }
        }

        public async void StartPing()
        {
            while (this.State == ConnectionState.Connected)
            {
                try
                {
                    await Task.Delay(_keepAliveInterval);

                    if (this.State == ConnectionState.Connected)
                    {
                        if (_isSending)
                            continue;

                        LogTrace($"KeepAlive sent, left retries: {_leftKeepAliveRetries}");
                        await SendMessage(Message.KeepAliveMessage);
                    }
                }
                catch (Exception ex)
                {
                    _leftKeepAliveRetries--;
                    LogTrace($"KeepAlive failed, left retries: {_leftKeepAliveRetries}");
                    if (_leftKeepAliveRetries == 0)
                    {
                        Close(false);
                    }
                }
            }
        }

        public async Task SendOneWay(Message message)
        {
            LogTrace("Send OneWay", message);

            if (message.MessageType != MessageType.OneWayRequest)
            {
                throw new Exception($"Invalid message wrapping");
            }

            await SendMessage(message);
        }

        public async Task SendResponse(Message message)
        {
            LogTrace("Send Response", message);

            if (message.MessageType != MessageType.Response)
            {
                throw new Exception($"Invalid message wrapping");
            }

            await SendMessage(message);
        }

        public async Task SendError(Message message)
        {
            LogTrace("Send Error", message);

            if (message.MessageType != MessageType.Error)
            {
                throw new Exception($"Invalid message wrapping");
            }

            await SendMessage(message);
        }

        public async Task<Message> SendRequest(Message message)
        {
            LogTrace("Sending Request", message);

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
            Close(new CloseBehavior(true, "Close or Dispose is called"));
        }

        async void Close(object closeBehavior)
        {
            var behavior = (CloseBehavior)closeBehavior;
            if (CanClose())
            {
                await _lock.WaitAsync();
                try
                {
                    if(CanClose())
                    {
                        LogTrace($"Closing because: {behavior.Reason}");
                        State = ConnectionState.Closing;
                        if (behavior.SendClose)
                        {
                            try
                            {
                                await SendMessage(Message.CloseMessage);
                            }
                            catch { }
                        }
                        await OnClose(CancellationToken.None);
                    }
                }
                catch(Exception ex)
                {
                    LogError(ex, "Graceful close failed");
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

        async Task SendMessage(Message message)
        {
            try
            {
                using (var cts = new CancellationTokenSource(_timeout))
                using (cts.Token.Register(Close, new CloseBehavior(true, "Send message failed")))
                {
                    _isSending = true;
                    await SendMessage(message, cts.Token);
                }
            }
            finally
            {
                _isSending = false;
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

        protected void LogTrace(string msg)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace($"{_connectionName}: {msg}");
        }

        protected void LogTrace(string msg, Message message)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace(MessageToString(msg, message));
        }

        protected void LogError(Exception ex, string error)
        {
            if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError(ex, $"{_connectionName}: {error}");
        }

        protected void LogError(Exception ex, Message message, string error)
        {
            if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError(ex, MessageToString(error, message));
        }

        string MessageToString(string message, Message msg)
        {
            return Concat(_connectionName, message, msg.MessageType, msg.Id, msg.Payload?.Length);
        }

        string Concat(params object[] objs)
        {
            if (objs != null && objs.Length > 0)
            {
                var sb = new StringBuilder();
                foreach (var obj in objs)
                {
                    if (obj != null)
                    {
                        sb.Append(obj).Append(" ");
                    }
                }
                var result = sb.ToString();
                return result;
            }
            return string.Empty;
        }
    }

    class CloseBehavior
    {
        public readonly bool SendClose;
        public readonly string Reason;

        public CloseBehavior(bool sendClose, string reason)
        {
            SendClose = sendClose;
            Reason = reason;
        }
    }
}
