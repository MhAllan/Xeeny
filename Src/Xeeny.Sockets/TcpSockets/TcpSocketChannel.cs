using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports;

namespace Xeeny.Sockets.TcpSockets
{
    public class TcpSocketChannel : ITransportChannel
    {
        readonly Socket _socket;
        readonly IPAddress _ipAddress;
        readonly int _port;
        readonly SocketFlags _flags;
        readonly bool _isClient;
        public TcpSocketChannel(Socket socket, IPAddress address, int port, SocketFlags flags)
        {
            _socket = socket;
            _ipAddress = address;
            _port = port;
            _flags = flags;
            _isClient = true;
        }

        public TcpSocketChannel(Socket socket, SocketFlags flags)
        {
            _socket = socket;
            _flags = flags;
            _isClient = false;
        }

        public async Task Connect(CancellationToken ct)
        {
            if(_isClient)
            {
                await _socket.ConnectAsync(_ipAddress, _port);
            }
        }

        public Task SendAsync(ArraySegment<byte> segment, CancellationToken ct)
        {
            return _socket.SendAsync(segment, _flags);
        }

        public Task<int> ReceiveAsync(ArraySegment<byte> segment, CancellationToken ct)
        {
            return _socket.ReceiveAsync(segment, _flags);
        }

        public void Close(CancellationToken ct)
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
