using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xeeny.Sockets.Messages;

namespace Xeeny.Sockets.TcpSockets
{
    public class TcpSocket : SocketBase
    {
        readonly Socket _socket;
        readonly IPAddress _remoteIP;
        readonly int _remotePort;
        readonly bool _isClient;

        public TcpSocket(Socket socket, IPSocketSettings settings, ILoggerFactory loggerFactory) 
            : base(settings, loggerFactory.CreateLogger(nameof(TcpSocket)))
        {
            _socket = socket;
            SetState();
        }

        public TcpSocket(Uri uri, IPSocketSettings settings, ILoggerFactory loggerFactory) 
            : base(settings, loggerFactory.CreateLogger(nameof(TcpSocket)))
        {
            var ip = SocketTools.GetIP(uri, settings.IPVersion);
            var family = ip.AddressFamily;
            var client = new TcpClient(family);

            _remoteIP = ip;
            _remotePort = uri.Port;
            _isClient = true;

            _socket = client.Client;
            _socket.SendBufferSize = settings.SendBufferSize;
            _socket.ReceiveBufferSize = settings.ReceiveBufferSize;

            _socket.NoDelay = true;

            if (family == AddressFamily.InterNetworkV6)
            {
                _socket.DualMode = true;
            }

            SetState();
        }

        void SetState()
        {
            if (_socket.Connected)
            {
                State = ConnectionState.Connected;
            }
        }

        protected override async Task OnConnect(CancellationToken ct)
        {
            if (!_isClient)
                throw new Exception("Can not call Connect on this socket because it is not client socket");

            await _socket.ConnectAsync(_remoteIP, _remotePort);
        }

        protected override async Task Send(IEnumerable<ArraySegment<byte>> segments, CancellationToken ct)
        {
            foreach (var segment in segments)
            {
                await _socket.SendAsync(segment, SocketFlags.None)
                             .ConfigureAwait(false);
            }
        }

        protected override async Task<byte[]> Receive(ArraySegment<byte> receiveBuffer, MessageParser parser, 
            CancellationToken ct)
        {
            using (var ms = new MemoryStream())
            {
                bool accepted = false;
                int msgSize = 0;
                int received = 0;
                do
                {
                    var size = await _socket.ReceiveAsync(receiveBuffer, SocketFlags.None)
                                            .ConfigureAwait(false);
                    if (!accepted)
                    {
                        parser.ValidateSize(receiveBuffer, out msgSize);
                        accepted = true;
                    }
                    await ms.WriteAsync(receiveBuffer.Array, 0, size);

                    received += size;
                }
                while (received < msgSize);

                return ms.ToArray();
            }
        }

        protected override void OnClose(CancellationToken ct)
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"{ConnectionId} Failed to shutdown", ex.Message);
            }
            try
            {
                _socket.Disconnect(false);
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"{ConnectionId} Failed to disconnect", ex.Message);
            }
            try
            {
                _socket.Close();
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"{ConnectionId} Failed to close", ex.Message);
            }
        }
    }
}
