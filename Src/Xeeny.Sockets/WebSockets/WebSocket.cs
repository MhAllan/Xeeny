using Microsoft.Extensions.Logging;
using Xeeny.Sockets.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Sockets.WebSockets
{
    public class WebSocket : SocketBase
    {
        System.Net.WebSockets.WebSocket _webSocket;

        public WebSocket(System.Net.WebSockets.WebSocket socket, SocketSettings settings, ILoggerFactory loggerFactory)
            : base(settings, loggerFactory.CreateLogger(nameof(WebSocket)))
        {
            _webSocket = socket;
            SetState();
        }

        public WebSocket(Uri uri, SocketSettings settings, ILoggerFactory loggerFactory)
            : base(uri, settings, loggerFactory.CreateLogger(nameof(WebSocket)))
        {
            _webSocket = new System.Net.WebSockets.ClientWebSocket();
            SetState();
        }

        void SetState()
        {
            var state = _webSocket.State;
            switch(state)
            {
                case WebSocketState.None: State = ConnectionState.None; break;
                case WebSocketState.Connecting: State = ConnectionState.Connecting; break;
                case WebSocketState.Open: State = ConnectionState.Connected; break;
                case WebSocketState.CloseSent: State = ConnectionState.Closing; break;
                case WebSocketState.CloseReceived: State = ConnectionState.Closing; break;
                case WebSocketState.Closed: State = ConnectionState.Closed; break;
                default: throw new NotSupportedException(state.ToString());
            }
        }

        protected override Task OnConnect(Uri uri, CancellationToken ct)
        {
            return ((ClientWebSocket)_webSocket).ConnectAsync(uri, ct);
        }

        protected override async void OnClose(CancellationToken ct)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session Ended", ct);
            }
            catch { }
            try
            {
                await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Session Ended", ct);
            }
            catch { }
        }

        protected override async Task Send(IEnumerable<ArraySegment<byte>> segments, CancellationToken ct)
        {
            foreach (var segment in segments)
            {
                await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, ct)
                                .ConfigureAwait(false);
            }
        }
        
        protected override async Task<byte[]> Receive(ArraySegment<byte> receiveBuffer, MessageParser parser,
            CancellationToken ct)
        {
            using (var ms = new MemoryStream())
            {
                bool accepted = false;

                WebSocketReceiveResult result;
                int msgSize = 0;
                int received = 0;
                do
                {
                    result = await _webSocket.ReceiveAsync(receiveBuffer, ct)
                                             .ConfigureAwait(false);

                    if (!accepted)
                    {
                        parser.ValidateSize(receiveBuffer, out msgSize);
                        accepted = true;
                    }

                    await ms.WriteAsync(receiveBuffer.Array, 0, result.Count);

                    received += result.Count;
                }
                while (received < msgSize);

                return ms.ToArray();
            }
        }
    }
}
