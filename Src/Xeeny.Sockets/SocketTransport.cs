using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xeeny.Transports;
using Xeeny.Transports.Channels;

namespace Xeeny.Sockets
{
    public class SocketTransport : TransportBase
    {
        readonly IMessageChannel _channel;

        public SocketTransport(Socket socket, SocketTransportSettings settings, ILoggerFactory loggerFactory)
            : base (settings, ConnectionSide.Server, loggerFactory.CreateLogger(nameof(SocketTransport)))
        {
            var transport = CreateTransport(socket, settings.SecuritySettings);
            _channel = CreateChannel(transport, settings);
        }

        public SocketTransport(Uri uri, SocketTransportSettings settings, ILoggerFactory loggerFactory)
            : base(settings, ConnectionSide.Client, loggerFactory.CreateLogger(nameof(SocketTransport)))
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

            var transport = CreateTransport(socket, ip, port, settings.SecuritySettings);
            _channel = CreateChannel(transport, settings);
        }

        ITransportChannel CreateTransport(Socket socket, IPAddress ip, int port, SecuritySettings securitySettings)
        {
            ITransportChannel transportChannel;
            if (securitySettings == null)
            {
                transportChannel = new SocketChannel(socket, ip, port, SocketFlags.None, this.ConnectionName);
            }
            else
            {
                var certName = securitySettings.CertificateName;
                var validationCallback = securitySettings.ValidationCallback;
                transportChannel = new SslSocketChannel(socket, ip, port, certName, validationCallback, this.ConnectionName);
            }
            return transportChannel;
        }

        ITransportChannel CreateTransport(Socket socket, SecuritySettings securitySettings)
        {
            ITransportChannel transportChannel;
            if (securitySettings == null)
            {
                transportChannel = new SocketChannel(socket, SocketFlags.None, this.ConnectionName);
            }
            else
            {
                var x509Certificate = securitySettings.X509Certificate;
                var validationCallback = securitySettings.ValidationCallback;
                transportChannel = new SslSocketChannel(socket, x509Certificate, validationCallback, this.ConnectionName);
            }
            return transportChannel;
        }

        IMessageChannel CreateChannel(ITransportChannel transportChannel, SocketTransportSettings settings)
        {
            var framingProtocol = settings.FramingProtocol;
            switch (framingProtocol)
            {
                case FramingProtocol.SerialFragments:
                    {
                        return new SerialMessageStreamChannel(transportChannel, settings);
                    }
                case FramingProtocol.ConcurrentFragments:
                    {
                        return new ConcurrentMessageStreamChannel(transportChannel, settings);
                    }
                case FramingProtocol.UnorderedConcurrentFragments:
                    {
                        return new UnorderedConcurrentMessageChannel(transportChannel, settings);
                    }
                default: throw new NotSupportedException(framingProtocol.ToString());
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

        protected override Task OnClose(CancellationToken ct)
        {
            return _channel.Close(ct);
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
