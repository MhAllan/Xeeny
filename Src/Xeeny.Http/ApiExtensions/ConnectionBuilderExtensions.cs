using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Api.Client;
using Xeeny.Http;
using Xeeny.Http.ApiExtensions;
using Xeeny.Transports;

public static class ConnectionBuilderExtensions
{
    public static TBuilder WithWebSocketTransport<TBuilder>(this TBuilder builder, string address,
        Action<WebSocketTransportSettings> options = null)
        where TBuilder : BaseConnectionBuilder
    {
        var settings = new WebSocketTransportSettings(ConnectionSide.Client);
        options?.Invoke(settings);

        builder.TransportFactory = new WebSocketTransportFactory(address, settings);

        return builder;
    }
}
