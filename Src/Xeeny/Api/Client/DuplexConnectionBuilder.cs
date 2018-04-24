using Xeeny.Connections;
using Xeeny.Descriptions;
using Xeeny.Dispatching;
using Xeeny.Messaging;
using Xeeny.Proxies.ProxyGeneration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Api.Client
{
    public class DuplexConnectionBuilder<TService, TCallback> : ConnectionBuilder<TService>
        where TCallback: new()
    {
        public event Action<TCallback> CallbackInstanceCreated;
        public event Action<TCallback> CallbackSessionInstanceRemoved;

        readonly InstanceMode _instanceMode;
        readonly TCallback _singleton;

        InstanceContextFactory<TCallback> instanceContextFactory;

        public DuplexConnectionBuilder(InstanceMode instanceMode)
            : base()
        {
            TypeDescription<TCallback>.ValidateAsCallbackObject();
            _instanceMode = instanceMode;
        }

        public DuplexConnectionBuilder(TCallback singleton) : this(InstanceMode.Single)
        {
            if (singleton == null)
                throw new ArgumentNullException(nameof(singleton));
            _singleton = singleton;
        }

        public override async Task<TService> CreateConnection(bool open = true)
        {
            Validate();

            var msgBuilder = new MessageBuilder(Serializer);

            if (_singleton != null)
            {
                instanceContextFactory = new InstanceContextFactory<TCallback>(_singleton, msgBuilder, LoggerFactory);
            }
            else
            {
                instanceContextFactory = new InstanceContextFactory<TCallback>(_instanceMode, msgBuilder, LoggerFactory);
            }

            instanceContextFactory.InstanceCreated += OnInstanceFactoryInstanceCreate;
            instanceContextFactory.SessionInstanceRemoved += OnInstanceFactorySessionInstanceRemoved;

            var transport = TransportFactory.CreateTransport(LoggerFactory);

            var duplexClient = new DuplexClientConnection(transport, msgBuilder, instanceContextFactory);

            var proxy = new ProxyEmitter<TService, DuplexClientConnection>(duplexClient).CreateProxy();

            if (open)
            {
                await ((IConnection)proxy).Connect();
            }

            return proxy;
        }

        private void OnInstanceFactoryInstanceCreate(IInstanceContextFactory factory, IInstanceContext context)
        {
            this.CallbackInstanceCreated?.Invoke((TCallback)context.Service);
        }

        private void OnInstanceFactorySessionInstanceRemoved(IInstanceContextFactory factory, IInstanceContext context)
        {
            this.CallbackSessionInstanceRemoved?.Invoke((TCallback)context.Service);
        }
    }
}
