using Xeeny.Dispatching;
using Xeeny.Messaging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Proxies.ProxyGeneration;
using Microsoft.Extensions.Logging;
using Xeeny.Transports;
using Xeeny.Transports.Messages;
using System.Threading;

namespace Xeeny.Connections
{
    public class ServerConnection : ConnectionBase, ICallbackGenerator
    {
        readonly IInstanceContextFactory _instanceContextFactory;
        readonly IMessageBuilder _msgBuilder;
        readonly ILogger _logger;

        readonly Type _callbackType;
        object _callback;

        internal ServerConnection(ITransport transport, IMessageBuilder msgBuilder, 
            IInstanceContextFactory instanceContextFactory,
            Type callbackType,
            ILogger logger) 
            
            : base(transport)
        {
            _instanceContextFactory = instanceContextFactory;
            _msgBuilder = msgBuilder;
            _callbackType = callbackType;
            _logger = logger;
        }

        public override async Task Connect(CancellationToken ct = default)
        {
            await this.Transport.Connect(ct);
            this.Transport.Listen();
        }

        protected override async void OnRequestReceived(ITransport transport, Message message)
        {
            var msgType = message.MessageType;
            switch (msgType)
            {
                case MessageType.Connect:
                    {
                        /*TODO*/
                        break;
                    }

                case MessageType.OneWayRequest:
                    {
                        try
                        {
                            var instanceContext = _instanceContextFactory.CreateInstanceContext(this);
                            await instanceContext.HandleRequest(message, this);
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, $"Failed to handle {msgType.ToString()}");
                        }
                        break;
                    }

                case MessageType.Request:
                    {
                        RequestHandleResult result;
                        try
                        {
                            var instanceContext = _instanceContextFactory.CreateInstanceContext(this);
                            result = await instanceContext.HandleRequest(message, this);
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, $"Failed to handle {msgType.ToString()}");
                            var error = _msgBuilder.CreateError(message.Id, "Server Error");
                            result = new RequestHandleResult(error, true);
                        }

                        if (result.HasResponse)
                        {
                            await this.Transport.SendMessage(result.Response);
                        }
                        break;
                    }

                default:
                    {
                        throw new Exception($"Wrong protocol usage, Server can't accept messages of type {msgType}");
                    }
            }

        }

        public void SendOneWay(string operation, params object[] parameters)
        {
            var request = _msgBuilder.CreateOneWayRequest(operation, parameters);
            Transport.SendMessage(request);
        }

        public async Task SendOneWayAsync(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateOneWayRequest(operation, parameters);
            await Transport.SendMessage(msg);
        }

        public TCallback GetCallback<TCallback>()
        {
            if(_callback == null)
            {
                if (_callbackType == null)
                {
                    throw new Exception("No callback was defined");
                }
                if (_callbackType != typeof(TCallback))
                {
                    throw new Exception($"Callback must be of type:  {_callbackType}");
                }
                lock(this)
                {
                    if(_callback == null)
                    {
                        _callback = new ProxyEmitter<TCallback, ServerConnection>(this).CreateProxy();
                    }
                }
            }
            return (TCallback)_callback;
        }

        public IConnection GetConnection()
        {
            return this;
        }

        void LogError(Exception ex, string error)
        {
            if(_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, $"{ConnectionName}: {error}");
            }
        }
    }
}
