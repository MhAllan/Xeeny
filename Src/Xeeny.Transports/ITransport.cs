using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Transports
{
    public interface ITransport : IConnectionObject
    {
        string ConnectionName { get; }
        event Action<ITransport, Message> RequestReceived;

        Task SendOneWay(Message message);

        Task<Message> SendRequest(Message message);
        Task SendResponse(Message message);
        Task SendError(Message message);

        void StartPing();
        void Listen();
    }
}
