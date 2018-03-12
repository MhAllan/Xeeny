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

        public ConnectionState State => Socket.State;

        public string ConnectionId => Socket.ConnectionId;
        public string ConnectionName => Socket.ConnectionName;

        protected readonly ITransport Socket;

        public ConnectionBase(ITransport socket)
        {
            Socket = socket;
            Socket.StateChanged += OnSocketStateChanged;
            Socket.RequestReceived += OnRequestReceived;
        }

        protected virtual void OnRequestReceived(ITransport socket, Message message)
        {
            //Nothing, Implement in subclasses
        }

        private void OnSocketStateChanged(IConnectionObject obj)
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
            this.Socket.Close();
        }

        public void Dispose()
        {
            this.Socket.Dispose();
        }
    }
}
