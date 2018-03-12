﻿using Microsoft.Extensions.Logging;
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
                    Logger.LogTrace($"{_connectionName} State changed to {value}");
                    OnStateChanged();
                }
            }
        }

        protected abstract int MinMessageSize { get; }
        protected int MaxMessageSize => _maxMessageSize;

        readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        byte _leftKeepAliveRetries = 0;
        bool _isSending = false;

        readonly ResponseManager _responseManager = new ResponseManager();

        readonly int _timeout;
        readonly int _receiveTimeout;
        readonly int _keepAliveInterval;
        readonly byte _keepAliveRetries;

        readonly int _maxMessageSize;
        readonly int _sendBufferSize;
        readonly int _receiveBufferSize;

        readonly string _id;
        readonly string _connectionName;

        protected readonly ILogger Logger;

        public TransportBase(TransportSettings settings, ILogger logger)
        {
            var maxMessageSize = settings.MaxMessageSize;
            var sendBufferSize = settings.SendBufferSize;
            var receiveBufferSize = settings.ReceiveBufferSize;

            if (maxMessageSize <= MinMessageSize)
            {
                throw new Exception($"settings property {nameof(settings.MaxMessageSize)} must be larger " +
                    $"then {MinMessageSize}");
            }
            if (sendBufferSize <= MinMessageSize)
            {
                throw new Exception($"settings property {nameof(settings.SendBufferSize)} must be larger " +
                    $"then {MinMessageSize}");
            }
            if (receiveBufferSize <= MinMessageSize)
            {
                throw new Exception($"settings property {nameof(settings.ReceiveBufferSize)} must be larger " +
                    $"then {MinMessageSize}");
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
            var nameFormatter = settings.ConnectionNameFormatter;
            _connectionName = nameFormatter == null ? $"Connection ({_id})" : nameFormatter(_id);

            Logger = logger;
        }

        protected abstract Task OnConnect(CancellationToken ct);
        protected abstract void OnClose(CancellationToken ct);
        protected abstract void OnKeepAlivedReceived(Message message);
        protected abstract void OnAgreementReceived(Message message);
        protected abstract Task SendMessage(Message message, byte[] sendBuffer, CancellationToken ct);
        protected abstract Task<Message> ReceiveMessage(byte[] receiveBuffer, CancellationToken ct);

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
                    Logger.LogError(ex, $"Connection failed");
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
            var receiveBuffer = ArrayPool<byte>.Shared.Rent(_receiveBufferSize);
            try
            {
                while (this.State == ConnectionState.Connected)
                {
                    Logger.LogTrace($"{_connectionName}: receiving");

                    using (var cts = new CancellationTokenSource(_receiveTimeout))
                    using (var reg = cts.Token.Register(Close, new CloseBehavior(true, "Receive timeout")))
                    {
                        var message = await ReceiveMessage(receiveBuffer, cts.Token);
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
                    Logger.LogError(ex, $"{_connectionName} stopped receiving");
                    Close(new CloseBehavior(true, $"Listenning error {ex.Message}"));
                }
                else
                {
                    Logger.LogTrace($"{_connectionName} stopped receiving");
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(receiveBuffer);
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

                        Logger.LogTrace($"{_connectionName} pinging, left retries: {_leftKeepAliveRetries}");
                        await SendMessage(Message.KeepAliveMessage);
                    }
                }
                catch (Exception ex)
                {
                    _leftKeepAliveRetries--;
                    Logger.LogTrace($"{_connectionName} pinging failed, left retries: {_leftKeepAliveRetries}");
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
                        Logger.LogTrace($"{ConnectionName} is closing because: {behavior.Reason}");
                        State = ConnectionState.Closing;
                        if (behavior.SendClose)
                        {
                            try
                            {
                                await SendMessage(Message.CloseMessage);
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

        async Task SendMessage(Message message)
        {
            var sendBuffer = ArrayPool<byte>.Shared.Rent(_sendBufferSize);
            try
            {
                using (var cts = new CancellationTokenSource(_timeout))
                using (cts.Token.Register(Close, new CloseBehavior(true, "Send message failed")))
                {
                    _isSending = true;
                    await SendMessage(message, sendBuffer, cts.Token);
                }
            }
            finally
            {
                _isSending = false;
                ArrayPool<byte>.Shared.Return(sendBuffer);
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
            Logger.LogTrace("Started", _connectionName, msg.MessageType, msg.Id, msg.Payload?.Length);
        }

        void LogEnded(Message msg)
        {
            Logger.LogTrace("Ended", _connectionName, msg.MessageType, msg.Id, msg.Payload?.Length);
        }

        void LogError(Exception ex, Message msg, string message)
        {
            Logger.LogError(ex, message, _connectionName, msg.MessageType, msg.Id, msg.Payload?.Length);
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
