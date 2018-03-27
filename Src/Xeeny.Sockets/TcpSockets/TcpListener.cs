using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Transports;
using SNS = System.Net.Sockets;

namespace Xeeny.Sockets.TcpSockets
{
    public class TcpListener : IListener
    {
        public System.Net.Sockets.TcpListener Listener => _listener;

        readonly IPSocketSettings _socketSettings;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger _logger;

        readonly Uri _uri;
        System.Net.Sockets.TcpListener _listener;

        public TcpListener(Uri uri, IPSocketSettings settings, ILoggerFactory loggerFactory)
        {
            _uri = uri;
            _socketSettings = settings;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger(nameof(TcpListener));
            
        }

        public void Listen()
        {
            if (_listener != null)
            {
                Close();
            }

            var port = _uri.Port;
            if (_socketSettings.IPVersion == IPVersion.IPv6)
            {
                var endpint = new System.Net.IPEndPoint(System.Net.IPAddress.IPv6Any, port);
                _listener = new SNS.TcpListener(endpint);
                _listener.Server.SetSocketOption(SNS.SocketOptionLevel.IPv6, SNS.SocketOptionName.IPv6Only, false);
            }
            else if(_socketSettings.IPVersion == IPVersion.IPv4)
            {
                var endpint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, port);
                _listener = new SNS.TcpListener(endpint);
            }

            _listener.Start();
        }

        public async Task<ITransport> AcceptSocket()
        {
            var socket = await _listener.AcceptSocketAsync();
            var tcpSocket = new TcpSocket(socket);
            return new TcpTransport(tcpSocket, _socketSettings, _loggerFactory);
        }

        public void Close()
        {
            try
            {
                _listener.Stop();
            }
            catch
            { }
        }
    }
}
