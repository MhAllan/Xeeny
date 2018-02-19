using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Sockets.Protocol.Messages;

namespace Xeeny.Sockets
{
    public interface ISocket : IConnectionObject
    {
        string Id { get; }
        event Action<ISocket, Message> RequestReceived;

        Task SendOneWay(Message message);

        Task<Message> SendRequest(Message message);
        Task SendResponse(Message message);
        Task SendError(Message message);

        void StartPing();
        void Listen();
    }
}
