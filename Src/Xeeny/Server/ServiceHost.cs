using Xeeny.Connections;
using Xeeny.Dispatching;
using Xeeny.Messaging;
using Xeeny.Serialization;
using Xeeny.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Xeeny.Descriptions;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Xeeny.Server
{
    public class ServiceHost<TService> where TService : new()
    {
        public event Action<TService> ServiceInstanceCreated;
        public event Action<TService> SessionInstanceRemove;

        public HostStatus Status { get; private set; } = HostStatus.Created;

        bool CanOpen => this.Status == HostStatus.Created || this.Status >= HostStatus.Closed;
        bool CanClose => this.Status == HostStatus.Opened || this.Status == HostStatus.Openning;

        CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        CancellationToken cancellationToken => _cancellationSource.Token;

        readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        readonly ReadOnlyCollection<IListener> _listeners;
        readonly IInstanceContextFactory _instanceContextFactory;
        readonly ISerializer _serializer;
        readonly IMessageBuilder _msgBuilder;
        readonly Type _callbackType;
        readonly ILogger _logger;

        private ServiceHost(IList<IListener> listeners, ISerializer serializer,
            Type callbackType, ILoggerFactory loggerFactory)
        {
            TypeDescription<TService>.ValidateAsService(callbackType);

            _listeners = new ReadOnlyCollection<IListener>(listeners);
            _serializer = serializer;
            _msgBuilder = new MessageBuilder(_serializer);
            _callbackType = callbackType;
            _logger = loggerFactory.CreateLogger("ServiceHost");
        }

        internal ServiceHost(IList<IListener> listeners, InstanceMode instanceMode, ISerializer serializer,
            Type callbackType, ILoggerFactory loggerFactory)
            :this(listeners, serializer, callbackType, loggerFactory)
        {
            _instanceContextFactory = new InstanceContextFactory<TService>(instanceMode, _msgBuilder, loggerFactory);
            _instanceContextFactory.InstanceCreated += OnInstanceFactoryInstanceCreate;
            _instanceContextFactory.SessionInstanceRemoved += OnInstanceFactorySessionInstanceRemoved;
        }

        internal ServiceHost(IList<IListener> listeners, TService singleton, ISerializer serializer,
            Type callbackType, ILoggerFactory loggerFactory)
            : this(listeners, serializer, callbackType, loggerFactory)
        {
            _instanceContextFactory = new InstanceContextFactory<TService>(singleton, _msgBuilder, loggerFactory);
            _instanceContextFactory.InstanceCreated += OnInstanceFactoryInstanceCreate;
            _instanceContextFactory.SessionInstanceRemoved += OnInstanceFactorySessionInstanceRemoved;
        }

        private void OnInstanceFactoryInstanceCreate(IInstanceContextFactory factory, IInstanceContext context)
        {
            this.ServiceInstanceCreated?.Invoke((TService)context.Service);
        }

        private void OnInstanceFactorySessionInstanceRemoved(IInstanceContextFactory factory, IInstanceContext context)
        {
            this.SessionInstanceRemove?.Invoke((TService)context.Service);
        }

        public async Task Open()
        {
            if (CanOpen)
            {
                await _lock.WaitAsync();

                try
                {
                    if (CanOpen)
                    {
                        Status = HostStatus.Openning;

                        foreach(var listener in _listeners)
                        {
                            listener.Listen();
                        }

                        Status = HostStatus.Opened;

                        StartAcceptingSockets();
                    }
                    else
                    {
                        throw new Exception($"Can not open host with Status: {Status.ToString()}");
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }
            else
            {
                throw new Exception($"Can not open host with Status: {Status.ToString()}");
            }
        }

        void StartAcceptingSockets()
        {
            foreach(var listener in _listeners)
            {
                StartAcceptingSockets(listener);
            }
        }

        async void StartAcceptingSockets(IListener listener)
        {
            while (this.Status == HostStatus.Opened && cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    var socket = await listener.AcceptSocket();
                    var proxy = new ServerConnection(socket, _msgBuilder, _instanceContextFactory, _callbackType, _logger);
                    await proxy.Connect();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not accept socket");
                }
            }
        }

        public async Task Close()
        {
            if (CanClose)
            {
                await _lock.WaitAsync();

                try
                {
                    if (CanClose)
                    {
                        Status = HostStatus.Closing;

                        if (_cancellationSource != null)
                        {
                            _cancellationSource.Cancel();
                            _cancellationSource = new CancellationTokenSource();
                        }

                        foreach (var listener in _listeners)
                        {
                            try
                            {
                                listener.Close();
                            }
                            catch { }
                        }
                    }
                }
                finally
                {
                    this.Status = HostStatus.Closed;
                    _lock.Release();
                }
            }
        }
    }
}
