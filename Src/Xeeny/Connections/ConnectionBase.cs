using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Xeeny.Sockets.Messages;
using Xeeny.Messaging;
using Xeeny.Sockets;

namespace Xeeny.Connections
{
    public abstract class ConnectionBase : IConnection
    {
        public event Action<IConnectionSession> SessionEnded;
        public event Action<IConnectionObject> StateChanged;

        public ConnectionState State => Socket.State;

        protected readonly string Id = Guid.NewGuid().ToString();

        protected readonly ISocket Socket;

        public ConnectionBase(ISocket socket)
        {
            Socket = socket;
            Socket.StateChanged += OnSocketStateChanged;
            Socket.RequestReceived += OnRequestReceived;
        }

        protected virtual void OnRequestReceived(ISocket arg1, Message arg2)
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
