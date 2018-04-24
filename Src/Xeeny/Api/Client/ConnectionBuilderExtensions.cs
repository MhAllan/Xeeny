using Xeeny.Api.Client.Extended;
using Xeeny.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports;

namespace Xeeny.Api.Client
{
    public static class ConnectionBuilderExtensions
    {
        public static TBuilder WithTcpTransport<TBuilder>(this TBuilder builder, string address,
            Action<SocketTransportSettings> options = null)
            where TBuilder : BaseConnectionBuilder
        {
            var settings = new SocketTransportSettings(ConnectionSide.Client);
            options?.Invoke(settings);

            builder.TransportFactory = new TransportFactory(address, SocketType.TCP, settings);

            return builder;
        }

        public static TBuilder WithCustomTransport<TBuilder>(this TBuilder builder, IXeenyTransportFactory transportFactory)
            where TBuilder : BaseConnectionBuilder
        {
            
            builder.TransportFactory = new TransportFactory(transportFactory);

            return builder;
        }

        public static TBuilder WithCustomTransport<TBuilder>(this TBuilder builder, ITransportFactory transportFactory)
            where TBuilder : BaseConnectionBuilder
        {

            builder.TransportFactory = transportFactory;

            return builder;
        }
    }
}
