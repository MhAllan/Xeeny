using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xeeny.Transports;

namespace Xeeny.Sockets.TcpSockets
{
    public class TcpSocket : SequentialStreamTransport
    {
        readonly Socket _socket;
        readonly IPAddress _remoteIP;
        readonly int _remotePort;

        public TcpSocket(Socket socket, IPSocketSettings settings, ILoggerFactory loggerFactory)
            : base (settings, ConnectionSide.Server, loggerFactory.CreateLogger(typeof(TcpSocket)))
        {
            _socket = socket;
            SetState();
        }

        public TcpSocket(Uri uri, IPSocketSettings settings, ILoggerFactory loggerFactory)
            : base(settings, ConnectionSide.Client, loggerFactory.CreateLogger(typeof(TcpSocket)))
        {
            _remoteIP = SocketTools.GetIP(uri, settings.IPVersion);
            _remotePort = uri.Port;
            var family = _remoteIP.AddressFamily;
            _socket = new Socket(family, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp);
            if (family == AddressFamily.InterNetworkV6)
            {
                _socket.DualMode = true;
            }

            _socket.SendBufferSize = settings.SendBufferSize;
            _socket.ReceiveBufferSize = settings.ReceiveBufferSize;

            _socket.NoDelay = true;

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
            if (ConnectionSide != ConnectionSide.Client)
                throw new Exception("Can not call Connect on this socket because it is not client socket");

            await _socket.ConnectAsync(_remoteIP, _remotePort);
        }

        protected override async Task Send(ArraySegment<byte> segment, CancellationToken ct)
        {
            await _socket.SendAsync(segment, SocketFlags.None)
                           .ConfigureAwait(false);
        }

        protected override async Task<int> Receive(ArraySegment<byte> segment, CancellationToken ct)
        {
            var read = await _socket.ReceiveAsync(segment, SocketFlags.None)
                                    .ConfigureAwait(false);

            return read;
        }

        protected override void OnClose(CancellationToken ct)
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                LogTrace($"Failed to Shutdown {ex.Message}");
            }
            try
            {
                _socket.Disconnect(false);
            }
            catch (Exception ex)
            {
                LogTrace($"Failed to Disconnect {ex.Message}");
            }
            try
            {
                _socket.Close();
            }
            catch (Exception ex)
            {
                LogTrace($"Failed to Close {ex.Message}");
            }
        }

        protected override void OnKeepAlivedReceived(Message message)
        {
            //nothing
        }

        protected override void OnAgreementReceived(Message message)
        {
            //nothing
        }
    }
}
