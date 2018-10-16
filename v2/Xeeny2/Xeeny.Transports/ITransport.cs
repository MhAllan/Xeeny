using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Transports.Connections;
using Xeeny.Transports.Messages;

namespace Xeeny.Transports
{
    public delegate void RequestReceived(ITransport transport, Message message);
    public delegate void NegotiateMessageReceived(ITransport transport, Message message);

    public interface ITransport : IConnection
    {
        event RequestReceived RequestReceived;
        event NegotiateMessageReceived NegotiateMessageReceived;

        Task SendMessage(Message message);
        Task<Message> Invoke(Message message);

        void StartPing();
        void Listen();
    }
}
