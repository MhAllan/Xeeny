using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.ResponseProviders;
using Xeeny.Transports.Channels;
using Xeeny.Transports.Connections;
using Xeeny.Transports.Messages;

namespace Xeeny.Transports
{
    public abstract class Transport : ITransport
    {
        public event RequestReceived RequestReceived;
        public event ConnectionStateChanged StateChanged;
        public event NegotiateMessageReceived NegotiateMessageReceived;

        public string ConnectionId
        {
            get => Channel.ConnectionId;
            internal set => Channel.ConnectionId = value;
        }

        public string ConnectionName
        {
            get => Channel.ConnectionName;
            internal set => Channel.ConnectionName = value;
        }

        public ILoggerFactory LoggerFactory
        {
            get => Channel.LoggerFactory;
            internal set => Channel.LoggerFactory = value;
        }

        public ConnectionSide ConnectionSide => Channel.ConnectionSide;

        public TransportChannel Channel { get; }

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

        static readonly byte[] _keepAliveMessage = Message.KeepAlive.GetData();

        readonly SemaphoreSlim _stateLock = new SemaphoreSlim(1, 1);

        public TransportSettings Settings { get; }
        public TimeSpan Timeout { get; }
        public TimeSpan ReceiveTimeout { get; }
        public TimeSpan KeepAliveInterval { get; }

        readonly ResponseProvider<Message> _responseProvider = new ResponseProvider<Message>();
        readonly ILogger _logger;

        bool _isSending;

        public Transport(TransportChannel channel, TransportSettings settings, ILoggerFactory loggerFactory)
        {
            Channel = channel;

            Timeout = settings.Timeout;
            ReceiveTimeout = settings.ReceiveTimeout;
            KeepAliveInterval = settings.KeepAliveInterval;

            ConnectionId = Guid.NewGuid().ToString();
            var nameFormatter = settings.ConnectionNameFormatter;
            ConnectionName = ConnectionSide == ConnectionSide.Client ?
                                nameFormatter == null ?
                                $"Connection ({ConnectionId})" : nameFormatter(ConnectionId) :
                                nameFormatter == null ?
                                $"Server Connection ({ConnectionId})" : nameFormatter(ConnectionId);

            LoggerFactory = loggerFactory;

            _logger = LoggerFactory.CreateLogger(this.GetType());

            Settings = settings;
        }

        protected abstract Task SendMessage(byte[] message, CancellationToken ct);
        protected abstract Task<byte[]> ReceiveMessage(CancellationToken ct);

        public async Task Connect(CancellationToken ct = default)
        {
            if (CanConnect())
            {
                await _stateLock.WaitAsync(ct);
                try
                {
                    if (CanConnect())
                    {
                        LogTrace("Connecting...");
                        State = ConnectionState.Connecting;
                        using (ct.Register(TimeoutClose, new CloseBehavior(false, "Connect Timeout")))
                        {
                            await Channel.Connect(ct);
                            State = ConnectionState.Connected;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "Connection failed");
                    await Close(new CloseBehavior(false, "Connection failed"), default);
                    throw;
                }
                finally
                {
                    _stateLock.Release();
                }
            }
        }

        public async void Listen()
        {
            try
            {
                while (State == ConnectionState.Connected)
                {
                    LogTrace($"Receiving...");

                    using (var cts = new CancellationTokenSource(ReceiveTimeout))
                    using (cts.Token.Register(TimeoutClose, new CloseBehavior(true, "Receive timeout")))
                    {
                        var msgBytes = await ReceiveMessage(cts.Token);

                        var message = new Message(msgBytes);

                        cts.Token.ThrowIfCancellationRequested(); //in case channels implementation is bad

                        LogTrace("Received message", message);

                        var messageType = message.MessageType;

                        switch (messageType)
                        {
                            case MessageType.Negotiate:
                                {
                                    OnNegotiateReceived(message);
                                    break;
                                }
                            case MessageType.KeepAlive:
                                {
                                    OnKeepAliveReceived(message);
                                    break;
                                }
                            case MessageType.Close:
                                {
                                    await Close(new CloseBehavior(false, "Close message received"), default);
                                    break;
                                }
                            case MessageType.Request:
                                {
                                    OnRequestReceived(message);
                                    break;
                                }
                            case MessageType.Response:
                            case MessageType.Error:
                                {
                                    _responseProvider.SetResponse(message.Id, message);
                                    break;
                                }
                            default: throw new NotSupportedException(messageType.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (State == ConnectionState.Connected)
                {
                    LogError(ex, "Stopped receiving");
                    await Close(new CloseBehavior(true, $"Listenning error {ex.Message}"));
                }
                else
                {
                    LogTrace($"stopped receiving");
                }
            }
        }

        public async void StartPing()
        {
            while (State == ConnectionState.Connected)
            {
                try
                {
                    await Task.Delay(KeepAliveInterval);

                    if (State == ConnectionState.Connected)
                    {
                        if (_isSending)
                            continue;

                        using (var cts = new CancellationTokenSource(Timeout))
                        using (cts.Token.Register(TimeoutClose, new CloseBehavior(false, "KeepAlive Timeout")))
                        {
                            await SendMessage(_keepAliveMessage, cts.Token);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "KeepAlive failed");
                    await Close(new CloseBehavior(false, $"Sending KeepAlive failed"), default);
                }
            }
        }

        public async Task SendMessage(Message message)
        {
            LogTrace("Sending", message);

            try
            {
                using (var cts = new CancellationTokenSource(Timeout))
                using (cts.Token.Register(TimeoutClose, new CloseBehavior(true, "Send message failed")))
                {
                    _isSending = true;
                    var msg = message.GetData();
                    await SendMessage(msg, cts.Token);
                }
            }
            finally
            {
                _isSending = false;
            }
        }

        public async Task<Message> Invoke(Message message)
        {
            LogTrace("Invoking", message);

            var context = _responseProvider.CreateResponseContext(message.Id, Timeout);
            try
            {
                await SendMessage(message);

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
                _responseProvider.RemoveResponseContext(context);
            }
        }

        public virtual Task Close(CancellationToken ct)
        {
            return Close(new CloseBehavior(true, "Close or Dispose is called"), ct);
        }

        async void TimeoutClose(object closeBehavior)
        {
            await Close((CloseBehavior)closeBehavior);
        }

        async Task Close(CloseBehavior closeBehavior, CancellationToken ct = default)
        {
            if (CanClose())
            {
                await _stateLock.WaitAsync(ct);
                try
                {
                    if (CanClose())
                    {
                        LogTrace($"Closing because: {closeBehavior.Reason}");
                        State = ConnectionState.Closing;
                        if (closeBehavior.SendClose)
                        {
                            try
                            {
                                await SendMessage(Message.Close);
                            }
                            catch { }
                            await Channel.Close(ct);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "Graceful close failed");
                }
                finally
                {
                    State = ConnectionState.Closed;
                    _stateLock.Release();
                }
            }
        }

        protected virtual void OnStateChanged()
        {
            StateChanged?.Invoke(this);
        }

        protected virtual void OnKeepAliveReceived(Message msg)
        {
        }

        protected virtual void OnNegotiateReceived(Message msg)
        {
            NegotiateMessageReceived?.Invoke(this, msg);
        }

        protected virtual void OnRequestReceived(Message message)
        {
            RequestReceived?.Invoke(this, message);
        }

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

        protected void LogTrace(string msg)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace($"{ConnectionName}: {msg}");
        }

        protected void LogTrace(string msg, Message message)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace(MessageToString(msg, message));
        }

        protected void LogError(Exception ex, string error)
        {
            if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError(ex, $"{ConnectionName}: {error}");
        }

        protected void LogError(Exception ex, Message message, string error)
        {
            if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError(ex, MessageToString(error, message));
        }

        string MessageToString(string message, Message msg)
        {
            return Concat(ConnectionName, message, msg.MessageType, msg.Id, msg.Payload?.Length);

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

        public void Dispose() => Close(default);
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
