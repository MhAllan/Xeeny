using Xeeny.Api.Client.Extended;
using Xeeny.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Api.Client
{
    public static class ConnectionBuilderExtensions
    {
        public static TBuilder WithTcpTransport<TBuilder>(this TBuilder builder, string address,
            Action<IPSocketSettings> options = null)
            where TBuilder : BaseConnectionBuilder
        {
            var settings = new IPSocketSettings();
            options?.Invoke(settings);

            builder.SocketFactory = new SocketFactory(address, SocketType.TCP, settings);

            return builder;
        }

        public static TBuilder WithWebSocketTransport<TBuilder>(this TBuilder builder, string address,
            Action<SocketSettings> options = null)
            where TBuilder : BaseConnectionBuilder
        {
            var settings = new SocketSettings();
            options?.Invoke(settings);

            builder.SocketFactory = new SocketFactory(address, SocketType.WebSocket, settings);

            return builder;
        }

        public static TBuilder WithCustomTransport<TBuilder>(this TBuilder builder, IXeenySocketFactory socketFactory)
            where TBuilder : BaseConnectionBuilder
        {
            
            builder.SocketFactory = new SocketFactory(socketFactory);

            return builder;
        }

        public static TBuilder WithCustomTransport<TBuilder>(this TBuilder builder, ISocketFactory socketFactory)
            where TBuilder : BaseConnectionBuilder
        {

            builder.SocketFactory = socketFactory;

            return builder;
        }
    }
}
