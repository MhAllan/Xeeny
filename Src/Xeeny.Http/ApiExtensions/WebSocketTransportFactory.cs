using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Api.Client;
using Xeeny.Transports;

namespace Xeeny.Http.ApiExtensions
{
    class WebSocketTransportFactory : ITransportFactory
    {
        readonly WebSocketTransportSettings _settings;
        string _address;

        public WebSocketTransportFactory(string address, WebSocketTransportSettings settings)
        {
            _address = address;
            _settings = settings;
        }

        public ITransport CreateTransport(ILoggerFactory loggerFactory)
        {
            return WebSocketTools.CreateWebSocketTransport(_address, _settings, loggerFactory);
        }
    }
}
