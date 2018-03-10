using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Transports;

namespace Xeeny.Sockets.WebSockets
{
    public class WebSocketListener : IListener
    {
        public HttpListener Listener => _listener;

        readonly Uri _uri;
        readonly TransportSettings _socketSettings;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger _logger;

        HttpListener _listener;

        public WebSocketListener(Uri uri, TransportSettings settings, ILoggerFactory loggerFactory)
        {
            _uri = uri;
            _socketSettings = settings;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger(nameof(WebSocketListener));
        }

        public void Listen()
        {
            if (_listener != null)
            {
                Close();
            }

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{_uri.Port}/");
            _listener.Start();
        }

        public async Task<ITransport> AcceptSocket()
        {
            var context = await _listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                var wsContext = await context.AcceptWebSocketAsync(null);

                var socket = wsContext.WebSocket;

                return new WebSocket(socket, _socketSettings, _loggerFactory);
            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                throw new Exception("Not WebSocket Request");
            }
        }

        public void Close()
        {
            try
            {
                _listener.Stop();
            }
            catch
            { }
            try
            {
                _listener.Close();
            }
            catch
            { }
            try
            {
                _listener.Abort();
            }
            catch { }
        }
    }
}
