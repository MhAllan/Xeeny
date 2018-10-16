using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Xeeny.Api.Client;
using Xeeny.Sockets.TcpTransports.Channels;
using Xeeny.Transports;

namespace Xeeny.Sockets.TcpTransports.Client
{
    class TcpClientTransportFactory : ITransportFactory
    {
        readonly TcpClientSettings _socketClientSettings;
        readonly MessageTransportSettings _transportSettings;
        readonly ILoggerFactory _loggerFactory;

        public TcpClientTransportFactory(TcpClientTransportSettings settings, ILoggerFactory loggerFactory)
        {
            _socketClientSettings = settings.SocketSettings;
            _transportSettings = settings;
            _loggerFactory = loggerFactory;
        }

        public ITransport CreateTransport()
        {
            var uri = _socketClientSettings.Uri;
            if (uri.Scheme != "tcp" && uri.Scheme != Uri.UriSchemeNetTcp)
            {
                throw new Exception($"{nameof(_socketClientSettings.Uri)} must be valid tcp:// or net.tcp:// address");
            }

            var socket = new Socket(_socketClientSettings.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            var channel = new TcpChannel(socket, _socketClientSettings);
            var serialTransport = new SerialStreamTransport(channel, _transportSettings, _loggerFactory);

            var transport = new TcpTransport(serialTransport);

            return transport;
        }
    }
}
