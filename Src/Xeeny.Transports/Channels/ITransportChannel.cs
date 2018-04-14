using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports.Channels
{
    public interface ITransportChannel : IConnectChannel
    {
        Task SendAsync(ArraySegment<byte> segment, CancellationToken ct);
        Task<int> ReceiveAsync(ArraySegment<byte> segment, CancellationToken ct);
    }
}
