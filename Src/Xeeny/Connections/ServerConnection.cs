using Xeeny.Connections;
using Xeeny.Dispatching;
using Xeeny.Messaging;
using Xeeny.Sockets;
using Xeeny.Sockets.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Proxies.ProxyGeneration;
using Microsoft.Extensions.Logging;

namespace Xeeny.Connections
{
    public class ServerConnection : ConnectionBase, ICallbackGenerator
    {
        readonly IInstanceContextFactory _instanceContextFactory;
        readonly IMessageBuilder _msgBuilder;
        readonly ILogger _logger;

        readonly Type _callbackType;
        object _callback;

        internal ServerConnection(ISocket socket, IMessageBuilder msgBuilder, 
            IInstanceContextFactory instanceContextFactory,
            Type callbackType,
            ILogger logger) 
            
            : base(socket)
        {
            _instanceContextFactory = instanceContextFactory;
            _msgBuilder = msgBuilder;
            _callbackType = callbackType;
            _logger = logger;
        }

        public override Task Connect()
        {
            this.Socket.Listen();
            return Task.CompletedTask;
        }

        protected override async void OnRequestReceived(ISocket socket, Message message)
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
                            _logger.LogError(ex, $"Connection {Id} failed to handle {msgType.ToString()}");
                        }
                        break;
                    }

                case MessageType.Request:
                    {
                        Message? response = null;
                        try
                        {
                            var instanceContext = _instanceContextFactory.CreateInstanceContext(this);
                            response = await instanceContext.HandleRequest(message, this);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Connection {Id} failed to handle {msgType.ToString()}");
                            response = _msgBuilder.CreateError(message.Id, "Server Error");
                        }

                        if (response.HasValue)
                        {
                            await this.Socket.SendResponse(response.Value);
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
            //server doesn't wait.
            this.Socket.SendOneWay(request);
        }

        public async Task SendOneWayAsync(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateOneWayRequest(operation, parameters);
            await this.Socket.SendOneWay(msg);
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
    }
}
