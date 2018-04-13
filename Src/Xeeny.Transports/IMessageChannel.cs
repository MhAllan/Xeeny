using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports
{
    public interface IMessageChannel
    {
        Task Connect(CancellationToken ct);

        Task SendMessage(Message message, CancellationToken ct);
        Task<Message> ReceiveMessage(CancellationToken ct);

        void Close(CancellationToken ct);
    }
}
