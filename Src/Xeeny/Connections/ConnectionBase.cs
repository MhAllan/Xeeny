using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Xeeny.Messaging;
using Xeeny.Transports;

namespace Xeeny.Connections
{
    public abstract class ConnectionBase : IConnection
    {
        public event Action<IConnectionSession> SessionEnded;
        public event Action<IConnectionObject> StateChanged;

        public ConnectionState State => Transport.State;

        public string ConnectionId => Transport.ConnectionId;
        public string ConnectionName => Transport.ConnectionName;

        protected readonly ITransport Transport;

        public ConnectionBase(ITransport transport)
        {
            Transport = transport;
            Transport.StateChanged += OnTransportStateChanged;
            Transport.RequestReceived += OnRequestReceived;
        }

        protected virtual void OnRequestReceived(ITransport socket, Message message)
        {
            //Nothing, Implement in subclasses
        }

        private void OnTransportStateChanged(IConnectionObject obj)
        {
            this.StateChanged?.Invoke(this);

            if(State >= ConnectionState.Closing)
            {
                this.SessionEnded?.Invoke(this);
            }
        }

        public abstract Task Connect();

        public void Close()
        {
            this.Transport.Close();
        }

        public void Dispose()
        {
            this.Transport.Dispose();
        }
    }
}
