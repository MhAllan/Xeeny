using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xeeny.Transports.Channels;
using Xeeny.Transports.Connections;
using Xeeny.Transports.Messages;

namespace Xeeny.Transports
{
    public class DecoratorTransport : ITransport
    {
        public event RequestReceived RequestReceived;
        public event ConnectionStateChanged StateChanged;
        public event NegotiateMessageReceived NegotiateMessageReceived;

        public Transport Transport { get; protected set; }

        public ConnectionState State => Transport.State;

        public string ConnectionId => Transport.ConnectionId;

        public string ConnectionName => Transport.ConnectionName;

        public ConnectionSide ConnectionSide => Transport.ConnectionSide;

        public DecoratorTransport(Transport transport)
        {
            Transport = transport;

            Transport.RequestReceived += OnRequestReceived;
            Transport.StateChanged += OnStateChanged;
            Transport.NegotiateMessageReceived += OnNegotiateMessageReceived;
        }

        protected virtual void OnRequestReceived(ITransport transport, Message message)
        {
            RequestReceived?.Invoke(this, message);
        }

        protected virtual void OnStateChanged(IConnection connection)
        {
            StateChanged?.Invoke(this);
        }

        protected virtual void OnNegotiateMessageReceived(ITransport transport, Message message)
        {
            NegotiateMessageReceived?.Invoke(this, message);
        }

        public Task SendMessage(Message message)
        {
            return Transport.SendMessage(message);
        }

        public Task<Message> Invoke(Message message)
        {
            return Transport.Invoke(message);
        }

        public void StartPing()
        {
            Transport.StartPing();
        }

        public void Listen()
        {
            Transport.Listen();
        }

        public Task Connect(CancellationToken ct = default)
        {
            return Transport.Connect(ct);
        }

        public Task Close(CancellationToken ct = default)
        {
            return Transport.Close(ct);
        }

        public void Dispose()
        {
            Transport.Dispose();
        }
    }
}
