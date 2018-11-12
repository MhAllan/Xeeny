using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Xeeny.Transports.Channels;
using Xeeny.Transports.Connections;
using System.Threading.Tasks;
using System.Threading;

namespace Xeeny.Sockets.TcpTransports.Channels
{
    class TcpChannel : TransportChannel
    {
        internal Socket Socket { get; }

        readonly IPAddress _remoteIP;
        readonly int _remotePort;
        readonly SocketFlags _flags;

        public TcpChannel(Socket socket, TcpSocketSettings settings) : base(ConnectionSide.Server)
        {
            Socket = socket;
            _flags = settings.SocketFlags;
        }

        public TcpChannel(Socket socket, TcpClientSettings settings) : base(ConnectionSide.Client)
        {
            _remoteIP = settings.IP;
            _remotePort = settings.Port;
            Socket = socket;
            _flags = settings.SocketFlags;
        }

        public override async Task Connect(CancellationToken ct)
        {
            if (ConnectionSide == ConnectionSide.Client)
            {
                await Socket.ConnectAsync(_remoteIP, _remotePort)
                         .ConfigureAwait(false);
            }
        }

        public override async Task Send(ArraySegment<byte> data, CancellationToken ct)
        {
            if (data.Count > 0)
            {
                await Socket.SendAsync(data, _flags)
                            .ConfigureAwait(false);
            }
        }

        public override async Task<int> Receive(ArraySegment<byte> buffer, CancellationToken ct)
        {
            return await Socket.ReceiveAsync(buffer, _flags)
                            .ConfigureAwait(false);
        }

        public override Task Close(CancellationToken ct)
        {
            try
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Disconnect(false);
                Socket.Close();
            }
            finally
            {
                Socket.Dispose();
            }
            return Task.CompletedTask;
        }
    }
}
