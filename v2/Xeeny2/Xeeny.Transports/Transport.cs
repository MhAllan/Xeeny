//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Xeeny.ResponseProviders;
//using Xeeny.Transports.Channels;
//using Xeeny.Transports.MessageChannels;
//using Xeeny.Transports.Messages;

//namespace Xeeny.Transports
//{
//    public class Transport : ITransport
//    {
//        public event Action<ITransport, Message> RequestReceived;
//        public event ConnectionStateChanged StateChanged;

//        public string ConnectionId { get; }
//        public string ConnectionName { get; }
//        public ConnectionSide ConnectionSide => _channel.ConnectionSide;

//        ConnectionState _state;
//        public ConnectionState State
//        {
//            get => _state;
//            protected set
//            {
//                if (_state != value)
//                {
//                    _state = value;
//                    LogTrace($"State changed to {value}");
//                    OnStateChanged();
//                }
//            }
//        }

//        readonly SemaphoreSlim _stateLock = new SemaphoreSlim(1, 1);
//        readonly Message _keepAliveMessage = new Message(MessageType.KeepAlive);
//        readonly Message _closeMessage = new Message(MessageType.Close);

//        readonly TimeSpan _timeout;
//        readonly TimeSpan _receiveTimeout;
//        readonly TimeSpan _keepAliveInterval;
//        readonly ResponseProvider<Message> _responseProvider = new ResponseProvider<Message>();
//        readonly ILogger _logger;

//        bool _isSending;

//        readonly MessageChannel _channel;

//        public Transport(MessageChannel channel, TransportSettings settings, ILogger logger)
//        {
//            _timeout = settings.Timeout;
//            _receiveTimeout = settings.ReceiveTimeout;
//            _keepAliveInterval = settings.KeepAliveInterval;
//            _logger = logger;

//            ConnectionId = Guid.NewGuid().ToString();
//            var nameFormatter = settings.ConnectionNameFormatter;
//            ConnectionName = nameFormatter == null ? $"Connection ({ConnectionId})" : nameFormatter(ConnectionId);

//            _channel = channel;
//            _channel.ConnectionId = ConnectionId;
//            _channel.ConnectionName = ConnectionName;
//        }

//        public async Task Connect()
//        {
//            if (CanConnect())
//            {
//                await _stateLock.WaitAsync();
//                try
//                {
//                    if (CanConnect())
//                    {
//                        LogTrace("Connecting...");
//                        State = ConnectionState.Connecting;
//                        using (var cts = new CancellationTokenSource(_timeout))
//                        using (cts.Token.Register(Close, new CloseBehavior(false, "Connect Timeout")))
//                        {
//                            var ct = cts.Token;
//                            await _channel.Connect(ct);
//                            State = ConnectionState.Connected;
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    LogError(ex, "Connection failed");
//                    Close(new CloseBehavior(false, "Connection failed"));
//                    throw;
//                }
//                finally
//                {
//                    _stateLock.Release();
//                }
//            }
//        }

//        public async void Listen()
//        {
//            try
//            {
//                while (this.State == ConnectionState.Connected)
//                {
//                    LogTrace($"Receiving...");

//                    using (var cts = new CancellationTokenSource(_receiveTimeout))
//                    using (cts.Token.Register(Close, new CloseBehavior(true, "Receive timeout")))
//                    {
//                        var message = await _channel.Receive(cts.Token);

//                        cts.Token.ThrowIfCancellationRequested(); //in case channels implementation is bad

//                        LogTrace("Received message", message);

//                        var messageType = message.MessageType;

//                        switch (messageType)
//                        {
//                            case MessageType.KeepAlive:
//                                {
//                                    break;
//                                }
//                            case MessageType.Close:
//                                {
//                                    Close(new CloseBehavior(false, "Close message received")); break;
//                                }
//                            case MessageType.Negotiate:
//                                {
//                                    break;
//                                }
//                            case MessageType.Request:
//                                {
//                                    OnRequestReceived(message);
//                                    break;
//                                }
//                            case MessageType.Response:
//                            case MessageType.Error:
//                                {
//                                    _responseProvider.SetResponse(message.Id, message);
//                                    break;
//                                }
//                            default: throw new NotSupportedException(messageType.ToString());
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                if (this.State == ConnectionState.Connected)
//                {
//                    LogError(ex, "Stopped receiving");
//                    Close(new CloseBehavior(true, $"Listenning error {ex.Message}"));
//                }
//                else
//                {
//                    LogTrace($"stopped receiving");
//                }
//            }
//        }

//        public async void StartPing()
//        {
//            while (this.State == ConnectionState.Connected)
//            {
//                try
//                {
//                    await Task.Delay(_keepAliveInterval);

//                    if (this.State == ConnectionState.Connected)
//                    {
//                        if (_isSending)
//                            continue;

//                        using (var cts = new CancellationTokenSource(_timeout))
//                        using (cts.Token.Register(Close, new CloseBehavior(false, "KeepAlive Timeout")))
//                        {
//                            await _channel.Send(_keepAliveMessage, cts.Token);
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    LogError(ex, "KeepAlive failed");
//                    Close(false);
//                }
//            }
//        }

//        public async Task SendOneWay(Message message)
//        {
//            LogTrace("Send OneWay", message);

//            //if (message.MessageType != MessageType.OneWayRequest)
//            //{
//            //    throw new Exception($"Invalid message wrapping");
//            //}

//            await SendMessage(message);
//        }

//        public async Task SendResponse(Message message)
//        {
//            LogTrace("Send Response", message);

//            if (message.MessageType != MessageType.Response)
//            {
//                throw new Exception($"Invalid message wrapping");
//            }

//            await SendMessage(message);
//        }

//        public async Task SendError(Message message)
//        {
//            LogTrace("Send Error", message);

//            if (message.MessageType != MessageType.Error)
//            {
//                throw new Exception($"Invalid message wrapping");
//            }

//            await SendMessage(message);
//        }

//        public async Task<Message> SendRequest(Message message)
//        {
//            LogTrace("Sending Request", message);

//            if (message.MessageType != MessageType.Request)
//            {
//                throw new Exception($"Message Id of message type {message.MessageType} can not be empty");
//            }

//            var context = _responseProvider.CreateResponseContext(message.Id, _timeout);
//            try
//            {
//                await SendMessage(message);

//                var response = context.GetResponse();

//                return response;
//            }
//            catch (Exception ex)
//            {
//                LogError(ex, message, "Failed to send and get response");
//                throw;
//            }
//            finally
//            {
//                _responseProvider.RemoveResponseContext(context);
//            }
//        }

//        public void Close()
//        {
//            Close(new CloseBehavior(true, "Close or Dispose is called"));
//        }

//        async void Close(object closeBehavior)
//        {
//            var behavior = (CloseBehavior)closeBehavior;
//            if (CanClose())
//            {
//                await _stateLock.WaitAsync();
//                try
//                {
//                    if (CanClose())
//                    {
//                        LogTrace($"Closing because: {behavior.Reason}");
//                        State = ConnectionState.Closing;
//                        if (behavior.SendClose)
//                        {
//                            try
//                            {
//                                await SendMessage(_closeMessage);
//                            }
//                            catch { }
//                            await _channel.Close(default);
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    LogError(ex, "Graceful close failed");
//                }
//                finally
//                {
//                    State = ConnectionState.Closed;
//                    _stateLock.Release();
//                }
//            }
//        }

//        public void Dispose() => Close();

//        bool CanConnect()
//        {
//            if (State == ConnectionState.Connecting || State == ConnectionState.Connected)
//                return false;

//            if (State != ConnectionState.None)
//                throw new Exception($"Can not connect, socket is closed");

//            return true;
//        }

//        bool CanClose()
//        {
//            return State < ConnectionState.Closing;
//        }

//        async Task SendMessage(Message message)
//        {
//            try
//            {
//                using (var cts = new CancellationTokenSource(_timeout))
//                using (cts.Token.Register(Close, new CloseBehavior(true, "Send message failed")))
//                {
//                    _isSending = true;
//                    await _channel.Send(message, cts.Token);
//                }
//            }
//            finally
//            {
//                _isSending = false;
//            }
//        }

//        protected virtual void OnStateChanged()
//        {
//            this.StateChanged?.Invoke(this);
//        }

//        protected virtual void OnRequestReceived(Message message)
//        {
//            this.RequestReceived?.Invoke(this, message);
//        }

//        protected void LogTrace(string msg)
//        {
//            if (_logger.IsEnabled(LogLevel.Trace))
//                _logger.LogTrace($"{ConnectionName}: {msg}");
//        }

//        protected void LogTrace(string msg, Message message)
//        {
//            if (_logger.IsEnabled(LogLevel.Trace))
//                _logger.LogTrace(MessageToString(msg, message));
//        }

//        protected void LogError(Exception ex, string error)
//        {
//            if (_logger.IsEnabled(LogLevel.Error))
//                _logger.LogError(ex, $"{ConnectionName}: {error}");
//        }

//        protected void LogError(Exception ex, Message message, string error)
//        {
//            if (_logger.IsEnabled(LogLevel.Error))
//                _logger.LogError(ex, MessageToString(error, message));
//        }

//        string MessageToString(string message, Message msg)
//        {
//            return Concat(ConnectionName, message, msg.MessageType, msg.Id, msg.Payload?.Length);
//        }

//        string Concat(params object[] objs)
//        {
//            if (objs != null && objs.Length > 0)
//            {
//                var sb = new StringBuilder();
//                foreach (var obj in objs)
//                {
//                    if (obj != null)
//                    {
//                        sb.Append(obj).Append(" ");
//                    }
//                }
//                var result = sb.ToString();
//                return result;
//            }
//            return string.Empty;
//        }
//    }

//    class CloseBehavior
//    {
//        public readonly bool SendClose;
//        public readonly string Reason;

//        public CloseBehavior(bool sendClose, string reason)
//        {
//            SendClose = sendClose;
//            Reason = reason;
//        }
//    }
//}
