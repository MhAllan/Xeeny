using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Sockets.TcpSockets
{
    public class TcpSocket : ISocket
    {
        public bool Connected => _socket.Connected;

        readonly Socket _socket;

        public TcpSocket(Socket socket)
        {
            _socket = socket;
        }

        public Task ConnectAsServer(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task ConnectAsClient(IPAddress ipAddress, int port, CancellationToken ct)
        {
            return _socket.ConnectAsync(ipAddress, port);
        }

        public Task SendAsync(ArraySegment<byte> segment, SocketFlags flags, CancellationToken ct)
        {
            return _socket.SendAsync(segment, flags);
        }

        public Task<int> ReceiveAsync(ArraySegment<byte> segment, SocketFlags flags, CancellationToken ct)
        {
            return _socket.ReceiveAsync(segment, flags);
        }

        public void Close()
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Disconnect(false);
                _socket.Close();
            }
            finally
            {
                _socket.Dispose();
            }
        }
    }
}
