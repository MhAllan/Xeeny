using Microsoft.Extensions.Logging;
using Xeeny.Connections;
using Xeeny.Messaging;
using System;
using System.Collections.Concurrent;

namespace Xeeny.Dispatching
{
    class InstanceContextFactory<TService> : IInstanceContextFactory where TService:new()
    {
        readonly ConcurrentDictionary<IConnectionSession, IInstanceContext> _contexts =
            new ConcurrentDictionary<IConnectionSession, IInstanceContext>();

        //not static, allowing multiple hosts on same machine
        IInstanceContext _singleton;

        readonly IMessageBuilder _msgBuilder;
        readonly InstanceMode _instanceMode;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger _logger;

        public event Action<IInstanceContextFactory, IInstanceContext> InstanceCreated;
        public event Action<IInstanceContextFactory, IInstanceContext> SessionInstanceRemoved;

        public InstanceContextFactory(InstanceMode instanceMode, IMessageBuilder msgBuilder, ILoggerFactory loggerFactory)
        {
            _instanceMode = instanceMode;
            _msgBuilder = msgBuilder;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger("InstanceContextFactory");
        }

        public InstanceContextFactory(TService singleton, IMessageBuilder msgBuilder, ILoggerFactory loggerFactory)
            : this(InstanceMode.Single, msgBuilder, loggerFactory)
        {
            _singleton = new InstanceContext<TService>(singleton, msgBuilder, loggerFactory);
        }

        public IInstanceContext CreateInstanceContext(IConnection connectionSession)
        {
            IInstanceContext result = null;
            if (_instanceMode == InstanceMode.Single)
            {
                if (_singleton == null)
                {
                    lock (this)
                    {
                        if (_singleton == null)
                            _singleton = NewInstanceContext();
                    }
                }
                result = _singleton;
            }
            else if (_instanceMode == InstanceMode.PerCall)
            {
                result = NewInstanceContext();
            }
            else if (_instanceMode == InstanceMode.PerConnection)
            {
                try
                {
                    result = _contexts[connectionSession];
                }
                catch
                {
                    connectionSession.SessionEnded -= OnSessionEnded;
                    connectionSession.SessionEnded += OnSessionEnded;
                    result = _contexts.AddOrUpdate(connectionSession, NewInstanceContext(), (s, d) => d);
                }
            }
            return result;
        }

        InstanceContext<TService> NewInstanceContext()
        {
            var service = new TService();
            var context = new InstanceContext<TService>(service, _msgBuilder, _loggerFactory);

            this.InstanceCreated?.Invoke(this, context);

            return context;
        }

        void OnSessionEnded(IConnectionSession connection)
        {
            if(_contexts.TryRemove(connection, out IInstanceContext context))
            {
                this.SessionInstanceRemoved?.Invoke(this, context);
            }
            else
            {
                //TODO log info
            }
        }
    }
}
