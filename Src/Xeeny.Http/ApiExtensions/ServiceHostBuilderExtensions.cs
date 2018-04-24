using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Api.Server;
using Xeeny.Http;
using Xeeny.Http.ApiExtensions;
using Xeeny.Transports;

public static class ServiceHostBuilderExtensions
{
    public static TBuilder AddWebSocketServer<TBuilder>(this TBuilder builder, string address,
        Action<WebSocketTransportSettings> options = null)
        where TBuilder : BaseServiceHostBuilder
    {
        var settings = new WebSocketTransportSettings(ConnectionSide.Server);
        options?.Invoke(settings);
        var listener = WebSocketTools.CreateWebSocketListener(address, settings, builder.LoggerFactory);
        builder.Listeners.Add(listener);
        return builder;
    }
}
