using Xeeny.Dispatching;
using Xeeny.Serialization;
using Xeeny.Server;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Linq;
using Xeeny.Transports;

namespace Xeeny.Api.Server
{ 
    public class ServiceHostBuilder<TService> : BaseServiceHostBuilder where TService: new()
    {
        protected internal readonly InstanceMode InstanceMode;
        protected internal readonly TService SingletonInstance;

        protected internal override List<IListener> Listeners { get; set; } = new List<IListener>();
        protected internal override ISerializer Serializer { get; set; } = new MessagePackSerializer();
        protected internal override ILoggerFactory LoggerFactory { get; set; } = new LoggerFactory();

        protected internal override Type CallbackType { get; set; }

        public ServiceHostBuilder(InstanceMode instanceMode)
        {
            InstanceMode = instanceMode;
        }

        public ServiceHostBuilder(TService singleton) : this (InstanceMode.Single)
        {
            if (singleton == null)
                throw new ArgumentNullException(nameof(singleton));
            SingletonInstance = singleton;
        }

        public ServiceHost<TService> CreateHost()
        {
            Validate();

            var host = SingletonInstance == null
                ? new ServiceHost<TService>(Listeners, InstanceMode, Serializer, CallbackType, LoggerFactory)
                : new ServiceHost<TService>(Listeners, SingletonInstance, Serializer, CallbackType, LoggerFactory);

            return host;
        }

        void Validate()
        {
            if (Listeners == null || !Listeners.Any())
            {
                throw new Exception($"No Servers found, user AddXXXServer methods to add servers");
            }
        }

        public ServiceHostBuilder<TService> WithCallback<TCallback>()
        {
            CallbackType = typeof(TCallback);
            return this;
        }
    }
}
