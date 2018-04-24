using Microsoft.Extensions.Logging;
using Xeeny.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports;

namespace Xeeny.Api
{
    static class SocketTools
    {
        public static SocketTransport CreateTcpTransport(string address, SocketTransportSettings settings, ILoggerFactory loggerFactory)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var uri = new Uri(address);
            if (uri.Scheme != "tcp" && uri.Scheme != Uri.UriSchemeNetTcp)
            {
                throw new Exception($"{nameof(address)} must be valid tcp:// or net.tcp:// address");
            }

            return new SocketTransport(uri, settings, loggerFactory);
        }

        public static IListener CreateTcpListener(string address, SocketTransportSettings settings, ILoggerFactory loggerFactory)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var uri = new Uri(address);
            if (uri.Scheme != "tcp" && uri.Scheme != Uri.UriSchemeNetTcp)
            {
                throw new Exception($"{nameof(address)} must be valid tcp:// or net.tcp:// address");
            }

            return new TcpListener(uri, settings, loggerFactory);
        }
    }
}
