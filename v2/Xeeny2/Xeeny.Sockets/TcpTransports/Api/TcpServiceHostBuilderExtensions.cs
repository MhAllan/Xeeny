using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Sockets;
using Xeeny.Sockets.TcpTransports.Channels;
using Xeeny.Sockets.TcpTransports.Server;

namespace Xeeny.Api.Server
{
    public static class TcpServiceHostBuilderExtensions
    {
        public static TBuilder AddTcpServer<TBuilder>(this TBuilder builder, string address,
            Action<TcpServerTransportSettings> options = null)
            where TBuilder : BaseServiceHostBuilder
        {
            var socketSettings = new TcpSocketSettings(new Uri(address));
            var settings = new TcpServerTransportSettings(socketSettings);
            options?.Invoke(settings);
            var listener = new TcpListener(settings, builder.LoggerFactory);
            builder.Listeners.Add(listener);
            return builder;
        }
    }
}
