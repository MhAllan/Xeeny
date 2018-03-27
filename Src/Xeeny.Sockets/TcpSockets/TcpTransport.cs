using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xeeny.Transports;

namespace Xeeny.Sockets.TcpSockets
{
    public class TcpTransport : SequentialStreamTransport
    {
        readonly ISocket _socket;
        readonly IPAddress _remoteIP;
        readonly int _remotePort;

        public TcpTransport(TcpSocket socket, IPSocketSettings settings, ILoggerFactory loggerFactory)
            : base (settings, ConnectionSide.Server, loggerFactory.CreateLogger(nameof(TcpTransport)))
        {
            _socket = socket;
            var securitySettings = settings.SecuritySettings;
            if (securitySettings.UseSsl)
            {
                var x509Certificate = securitySettings.X509Certificate;
                var certName = securitySettings.CertificateName;
                _socket = new SslSocket(_socket, x509Certificate, certName);
            }
            SetState();
        }

        public TcpTransport(Uri uri, IPSocketSettings settings, ILoggerFactory loggerFactory)
            : base(settings, ConnectionSide.Client, loggerFactory.CreateLogger(nameof(TcpTransport)))
        {
            _remoteIP = SocketTools.GetIP(uri, settings.IPVersion);
            _remotePort = uri.Port;
            var family = _remoteIP.AddressFamily;
            var socket = new Socket(family, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp);
            if (family == AddressFamily.InterNetworkV6)
            {
                socket.DualMode = true;
            }
            socket.SendBufferSize = settings.SendBufferSize;
            socket.ReceiveBufferSize = settings.ReceiveBufferSize;

            socket.NoDelay = true;

            _socket = new TcpSocket(socket);

            var securitySettings = settings.SecuritySettings;
            if (securitySettings.UseSsl)
            {
                var x509Certificate = securitySettings.X509Certificate;
                var certName = securitySettings.CertificateName;
                _socket = new SslSocket(_socket, x509Certificate, certName);
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
            switch(ConnectionSide)
            {
                case ConnectionSide.Server:
                    await _socket.ConnectAsServer(ct).ConfigureAwait(false);
                    break;
                case ConnectionSide.Client:
                    await _socket.ConnectAsClient(_remoteIP, _remotePort, ct).ConfigureAwait(false);
                    break;
                default:
                    throw new NotSupportedException(ConnectionSide.ToString());
            }
        }

        protected override async Task Send(ArraySegment<byte> segment, CancellationToken ct)
        {
            await _socket.SendAsync(segment, SocketFlags.None, ct)
                           .ConfigureAwait(false);
        }

        protected override async Task<int> Receive(ArraySegment<byte> segment, CancellationToken ct)
        {
            var read = await _socket.ReceiveAsync(segment, SocketFlags.None, ct)
                                    .ConfigureAwait(false);

            return read;
        }

        protected override void OnClose(CancellationToken ct)
        {
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
