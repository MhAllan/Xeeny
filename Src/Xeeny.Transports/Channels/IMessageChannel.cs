using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports.Channels
{
    public interface IMessageChannel : IConnectChannel
    {
        Task SendMessage(Message message, CancellationToken ct);
        Task<Message> ReceiveMessage(CancellationToken ct);
    }
}
