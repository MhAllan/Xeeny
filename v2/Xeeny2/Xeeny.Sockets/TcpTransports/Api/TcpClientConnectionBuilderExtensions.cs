using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Api.Client;
using Xeeny.Sockets;
using Xeeny.Sockets.TcpTransports.Channels;
using Xeeny.Sockets.TcpTransports.Client;
using Xeeny.Transports;

namespace Xeeny.Api.Client
{
    public static class TcpClientConnectionBuilderExtensions
    {
        public static TBuilder WithTcpTransport<TBuilder>(this TBuilder builder, string address,
            Action<TcpClientTransportSettings> options = null)
            where TBuilder : BaseConnectionBuilder
        {
            var socketClientSettings = new TcpClientSettings(new Uri(address));
            var settings = new TcpClientTransportSettings(socketClientSettings);

            options?.Invoke(settings);

            builder.TransportFactory = new TcpClientTransportFactory(settings, builder.LoggerFactory);

            return builder;
        }
    }
}
