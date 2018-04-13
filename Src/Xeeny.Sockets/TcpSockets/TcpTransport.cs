using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xeeny.Transports;

namespace Xeeny.Sockets.TcpSockets
{
    public class TcpTransport : TransportBase
    {
        readonly IMessageChannel _channel;

        public TcpTransport(Socket socket, IPSocketSettings settings, ILoggerFactory loggerFactory)
            : base (settings, loggerFactory.CreateLogger(nameof(TcpTransport)))
        {
            ITransportChannel transportChannel = null;
            var securitySettings = settings.SecuritySettings;
            if(securitySettings == null)
            {
                transportChannel = new TcpSocketChannel(socket, SocketFlags.None);
            }
            else
            {
                var x509Certificate = securitySettings.X509Certificate;
                var validationCallback = securitySettings.ValidationCallback;
                transportChannel = new SslSocketChannel(socket, x509Certificate, validationCallback);
            }

            var allowConcurrentMessages = settings.AllowConcurrentMessages;
            if(allowConcurrentMessages)
            {
                _channel = new ParallelStreamTransport(transportChannel, settings);
            }
            else
            {
                _channel = new SequentialStreamTransport(transportChannel, settings);
            }
        }

        public TcpTransport(Uri uri, IPSocketSettings settings, ILoggerFactory loggerFactory)
            : base(settings, loggerFactory.CreateLogger(nameof(TcpTransport)))
        {
            var ip = SocketTools.GetIP(uri, settings.IPVersion);
            var port = uri.Port;
            var family = ip.AddressFamily;
            var socket = new Socket(family, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp);
            if (family == AddressFamily.InterNetworkV6)
            {
                socket.DualMode = true;
            }
            socket.SendBufferSize = settings.SendBufferSize;
            socket.ReceiveBufferSize = settings.ReceiveBufferSize;

            socket.NoDelay = true;

            ITransportChannel transportChannel;

            var securitySettings = settings.SecuritySettings;
            if(securitySettings == null)
            {
                transportChannel = new TcpSocketChannel(socket, SocketFlags.None);
            }
            else
            {
                var certName = securitySettings.CertificateName;
                var validationCallback = securitySettings.ValidationCallback;
                transportChannel = new SslSocketChannel(socket, ip, port, certName, validationCallback);
            }

            var allowConcurrentMessages = settings.AllowConcurrentMessages;
            if (allowConcurrentMessages)
            {
                _channel = new ParallelStreamTransport(transportChannel, settings);
            }
            else
            {
                _channel = new SequentialStreamTransport(transportChannel, settings);
            }
        }

        protected override Task OnConnect(CancellationToken ct)
        {
            return _channel.Connect(ct);
        }

        protected override Task SendMessage(Message message, CancellationToken ct)
        {
            return _channel.SendMessage(message, ct);
        }

        protected override Task<Message> ReceiveMessage(CancellationToken ct)
        {
            return _channel.ReceiveMessage(ct);
        }

        protected override void OnClose(CancellationToken ct)
        {
            try
            {
                _channel.Close(ct);
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
