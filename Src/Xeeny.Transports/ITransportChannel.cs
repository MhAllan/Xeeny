using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports
{
    public interface ITransportChannel
    {
        Task Connect(CancellationToken ct);

        Task SendAsync(ArraySegment<byte> segment, CancellationToken ct);
        Task<int> ReceiveAsync(ArraySegment<byte> segment, CancellationToken ct);

        void Close(CancellationToken ct);
    }
}
