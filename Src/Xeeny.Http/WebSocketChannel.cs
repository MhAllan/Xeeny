using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports;
using Xeeny.Transports.Channels;

namespace Xeeny.Http
{
    class WebSocketChannel : ITransportChannel
    {
        public string ConnectionName => _connectionName;

        public ConnectionSide ConnectionSide => _connectionSide;

        readonly WebSocket _webSocket;
        readonly Uri _uri;
        readonly ConnectionSide _connectionSide;
        readonly string _connectionName;

        public WebSocketChannel(WebSocket webSocket, string connectionName)
        {
            _webSocket = webSocket;
            _connectionName = connectionName;
            _connectionSide = ConnectionSide.Server;
        }

        public WebSocketChannel(Uri uri, string connectionName)
        {
            _webSocket = new ClientWebSocket();
            _uri = uri;
            _connectionName = connectionName;
            _connectionSide = ConnectionSide.Client;
        }

        public async Task Connect(CancellationToken ct)
        {
            if(_connectionSide == ConnectionSide.Client)
            {
                var client = (ClientWebSocket)_webSocket;
                await client.ConnectAsync(_uri, ct)
                            .ConfigureAwait(false);
            }
        }

        public async Task SendAsync(ArraySegment<byte> segment, CancellationToken ct)
        {
            await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, ct)
                            .ConfigureAwait(false);
        }

        public async Task<int> ReceiveAsync(ArraySegment<byte> segment, CancellationToken ct)
        {
            var result = await _webSocket.ReceiveAsync(segment, ct)
                                        .ConfigureAwait(false);

            return result.Count;
        }

        public async Task Close(CancellationToken ct)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session Ended", ct)
                                .ConfigureAwait(false);
            }
            catch { }
            try
            {
                await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Session Ended", ct)
                                .ConfigureAwait(false);
            }
            catch { }
        }
    }
}
