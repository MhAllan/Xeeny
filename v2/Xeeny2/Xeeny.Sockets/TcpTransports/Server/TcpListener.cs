using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Sockets.TcpTransports.Channels;
using Xeeny.Transports;
using SNS = System.Net.Sockets;

namespace Xeeny.Sockets.TcpTransports.Server
{
    public class TcpListener : IListener
    {
        public SNS.TcpListener Listener { get; private set; }

        readonly TcpServerTransportSettings _settings;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger _logger;

        public TcpListener(TcpServerTransportSettings settings, ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger(nameof(TcpListener));
        }

        public void Listen()
        {
            if (Listener != null)
            {
                Close();
            }

            var socketSettings = _settings.SocketSettings;
            if (socketSettings.IPVersion == IPVersion.IPv6)
            {
                var endpint = new System.Net.IPEndPoint(System.Net.IPAddress.IPv6Any, socketSettings.Port);
                Listener = new SNS.TcpListener(endpint);
                Listener.Server.SetSocketOption(SNS.SocketOptionLevel.IPv6, SNS.SocketOptionName.IPv6Only, false);
            }
            else if (socketSettings.IPVersion == IPVersion.IPv4)
            {
                var endpint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, socketSettings.Port);
                Listener = new SNS.TcpListener(endpint);
            }

            Listener.Start();
        }

        public async Task<ITransport> AcceptConnection()
        {
            var socket = await Listener.AcceptSocketAsync();

            var channel = new TcpChannel(socket, _settings.SocketSettings);

            var serialTransport = new SerialStreamTransport(channel, _settings, _loggerFactory);

            var transport = new TcpTransport(serialTransport);

            return transport;
        }

        public void Close()
        {
            try
            {
                Listener.Stop();
            }
            catch
            { }
        }
    }
}
