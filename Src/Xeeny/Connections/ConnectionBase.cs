using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Xeeny.Messaging;
using Xeeny.Transports;
using Xeeny.Transports.Connections;
using Xeeny.Transports.Messages;
using System.Threading;

namespace Xeeny.Connections
{
    public abstract class ConnectionBase : IConnection
    {
        public event SessionEnded SessionEnded;
        public event ConnectionStateChanged StateChanged;

        public ConnectionState State => Transport.State;

        public string ConnectionId => Transport.ConnectionId;
        public string ConnectionName => Transport.ConnectionName;
        public ConnectionSide ConnectionSide => Transport.ConnectionSide;

        protected readonly ITransport Transport;

        public ConnectionBase(ITransport transport)
        {
            Transport = transport;
            Transport.StateChanged += OnTransportStateChanged;
            Transport.RequestReceived += OnRequestReceived;
        }

        protected abstract void OnRequestReceived(ITransport socket, Message message);

        void OnTransportStateChanged(Transports.Connections.IConnection connection)
        {
            StateChanged?.Invoke(this);

            if(State == ConnectionState.Closing)
            {
                SessionEnded?.Invoke(this);
            }
        }

        public abstract Task Connect(CancellationToken ct = default);

        public Task Close(CancellationToken ct = default)
        {
            return Transport.Close(ct);
        }

        public void Dispose()
        {
            this.Transport.Close();
        }
    }
}
