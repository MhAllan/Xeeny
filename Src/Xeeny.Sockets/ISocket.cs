using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Sockets
{
    public interface ISocket
    {
        bool Connected { get; }
        Stream Stream { get; }

        Task ConnectAsServer(CancellationToken ct);
        Task ConnectAsClient(IPAddress ipAddress, int port, CancellationToken ct);

        Task SendAsync(ArraySegment<byte> segment, SocketFlags flags, CancellationToken ct);
        Task<int> ReceiveAsync(ArraySegment<byte> segment, SocketFlags flags, CancellationToken ct);

        void Close();
    }
}
