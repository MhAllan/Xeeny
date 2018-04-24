using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using Xeeny.Transports;

namespace Xeeny.Http.ApiExtensions
{
    class WebSocketTools
    {
        public static WebSocketTransport CreateWebSocketTransport(string address, 
            WebSocketTransportSettings settings, ILoggerFactory loggerFactory)
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

            return new WebSocketTransport(uri, settings, loggerFactory);
        }

        public static IListener CreateWebSocketListener(string address, WebSocketTransportSettings settings,
            ILoggerFactory loggerFactory)
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
    }
}
