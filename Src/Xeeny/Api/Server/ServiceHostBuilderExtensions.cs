﻿using Xeeny.Api.Server.Extended;
using Xeeny.Sockets;
using Xeeny.Sockets.RawSockets;
using Xeeny.Sockets.TcpSockets;
using Xeeny.Sockets.WebSockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Api.Server
{
    public static class Extensions
    {
        public static TBuilder AddTcpServer<TBuilder>(this TBuilder builder, string address,
            Action<IPSocketSettings> options = null)
            where TBuilder : BaseServiceHostBuilder
        {
            var settings = new IPSocketSettings();
            options?.Invoke(settings);
            var listener = SocketTools.CreateTcpListener(address, settings, builder.LoggerFactory);
            builder.Listeners.Add(listener);
            return builder;
        }

        public static TBuilder AddWebSocketServer<TBuilder>(this TBuilder builder, string address,
            Action<SocketSettings> options = null)
            where TBuilder : BaseServiceHostBuilder
        {
            var settings = new SocketSettings();
            options?.Invoke(settings);
            var listener = SocketTools.CreateWebSocketListener(address, settings, builder.LoggerFactory);
            builder.Listeners.Add(listener);
            return builder;
        }

        public static TBuilder AddCustomServer<TBuilder>(this TBuilder builder, XeenyListener listener)
            where TBuilder : BaseServiceHostBuilder
        {
            builder.Listeners.Add(listener);
            return builder;
        }

        public static TBuilder AddCustomServer<TBuilder>(this TBuilder builder, IListener listener)
            where TBuilder : BaseServiceHostBuilder
        {
            builder.Listeners.Add(listener);
            return builder;
        }
    }
}
