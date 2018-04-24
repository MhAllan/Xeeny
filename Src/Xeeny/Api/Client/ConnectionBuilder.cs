using Xeeny.Messaging;
using Xeeny.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Descriptions;
using Xeeny.Connections;
using Xeeny.Proxies.ProxyGeneration;
using Microsoft.Extensions.Logging;

namespace Xeeny.Api.Client
{
    public class ConnectionBuilder<TService> : BaseConnectionBuilder
    {
        protected internal override ISerializer Serializer { get; set; } = new MessagePackSerializer();
        protected internal override ILoggerFactory LoggerFactory { get; set; } = new LoggerFactory();

        protected internal override ITransportFactory TransportFactory { get; set; }

        public ConnectionBuilder()
        {
            TypeDescription<TService>.ValidateAsContract(null);
        }

        public virtual async Task<TService> CreateConnection(bool open = true)
        {
            Validate();

            var msgBuilder = new MessageBuilder(Serializer);

            var transport = TransportFactory.CreateTransport(LoggerFactory);

            var client = new ClientConnection(transport, msgBuilder);

            var proxy = new ProxyEmitter<TService, ClientConnection>(client).CreateProxy();
            if (open)
            {
                await((IConnection)proxy).Connect();
            }

            return proxy;
        }

        private protected void Validate()
        {
            if (TransportFactory == null)
            {
                throw new Exception("No connection settings, use one WithXXXTransport methods to add settings");
            }
        }
    }
}
