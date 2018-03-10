using Microsoft.Extensions.Logging;
using Xeeny.Sockets;
using Xeeny.Sockets.TcpSockets;
using Xeeny.Sockets.WebSockets;
using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports;

namespace Xeeny.Api
{
    public static class SocketTools
    {
        public static WebSocket CreateWebSocket(string address, TransportSettings settings, ILoggerFactory loggerFactory)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.StartsWith("http://"))
            {
                address = address.Replace("http://", "ws://");
            }

            var uri = new Uri(address);
            if (uri.Scheme != "ws")
            {
                throw new Exception($"{nameof(address)} must be valid http:// or ws:// address");
            }

            return new WebSocket(uri, settings, loggerFactory);
        }

        public static IListener CreateWebSocketListener(string address, TransportSettings settings, ILoggerFactory loggerFactory)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (!address.EndsWith("/"))
                address += "/";

            var uri = new Uri(address);
            if (uri.Scheme != Uri.UriSchemeHttp)
            {
                throw new Exception($"{nameof(address)} must be valid http:// address");
            }

            return new WebSocketListener(uri, settings, loggerFactory);
        }

        public static TcpSocket CreateTcpSocket(string address, IPSocketSettings settings, ILoggerFactory loggerFactory)
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

            return new TcpSocket(uri, settings, loggerFactory);
        }

        public static IListener CreateTcpListener(string address, IPSocketSettings settings, ILoggerFactory loggerFactory)
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
