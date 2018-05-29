using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports;
using Xeeny.Transports.Channels;

namespace Xeeny.Sockets.TcpTransport
{
    public class TcpChannel : Channel
    {
        readonly Socket _socket;
        readonly IPAddress _remoteIP;
        readonly int _remotePort;
        readonly SocketFlags _flags;

        public TcpChannel(Socket socket, TcpSocketSettings settings) : base(ConnectionSide.Server)
        {
            _socket = socket;
            _flags = settings.SocketFlags;
        }

        public TcpChannel(Socket socket, TcpClientSettings settings) : base(ConnectionSide.Client)
        {
            _remoteIP = settings.IP;
            _remotePort = settings.Port;
            _socket = socket;
            _flags = settings.SocketFlags;
        }

        public override async Task Connect(CancellationToken ct)
        {
            if (ConnectionSide == ConnectionSide.Client)
            {
                await _socket.ConnectAsync(_remoteIP, _remotePort)
                         .ConfigureAwait(false);
            }
        }

        public override async Task Send(ArraySegment<byte> data, CancellationToken ct)
        {
            if (data.Count > 0)
            {
                await _socket.SendAsync(data, _flags)
                            .ConfigureAwait(false);
            }
        }

        public override async Task<int> Receive(ArraySegment<byte> buffer, CancellationToken ct)
        {
            return await _socket.ReceiveAsync(buffer, _flags)
                            .ConfigureAwait(false);
        }

        public override Task Close(CancellationToken ct)
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
            return Task.CompletedTask;
        }
    }
}
