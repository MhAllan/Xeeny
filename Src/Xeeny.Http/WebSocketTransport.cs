using Microsoft.Extensions.Logging;
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
    class WebSocketTransport : TransportBase
    {
        readonly IMessageChannel _channel;

        public WebSocketTransport(WebSocket webSocket, WebSocketTransportSettings settings, ILoggerFactory loggerFactory)
            : base(settings, ConnectionSide.Server, loggerFactory.CreateLogger(nameof(WebSocketTransport)))
        {
            var transport = new WebSocketChannel(webSocket, this.ConnectionName);
            _channel = CreateChannel(transport, settings);

            SetState(transport.State);
        }

        public WebSocketTransport(Uri uri, WebSocketTransportSettings settings, ILoggerFactory loggerFactory)
            : base(settings, ConnectionSide.Client, loggerFactory.CreateLogger(nameof(WebSocketTransport)))
        {
            var transport = new WebSocketChannel(uri, this.ConnectionName);
            _channel = CreateChannel(transport, settings);

            SetState(transport.State);
        }

        void SetState(WebSocketState state)
        {
            switch(state)
            {
                case WebSocketState.None: State = ConnectionState.None; break;
                case WebSocketState.Connecting: State = ConnectionState.Connecting; break;
                case WebSocketState.Open: State = ConnectionState.Connected; break;
                case WebSocketState.CloseSent:
                case WebSocketState.CloseReceived: State = ConnectionState.Closing; break;
                default: State = ConnectionState.Closed; break;
            }
        }

        IMessageChannel CreateChannel(ITransportChannel transport, WebSocketTransportSettings settings)
        {
            var framing = settings.FramingProtocol;
            switch(framing)
            {
                case FramingProtocol.SerialFragments: return new SerialMessageStreamChannel(transport, settings);
                case FramingProtocol.ConcurrentFragments: return new ConcurrentMessageStreamChannel(transport, settings);
                default: throw new NotSupportedException(framing.ToString());
            }
        }

        protected override Task OnConnect(CancellationToken ct)
        {
            return _channel.Connect(ct);
        }

        protected override Task SendMessage(Message message, CancellationToken ct)
        {
            return _channel.SendMessage(message, ct);
        }

        protected override Task<Message> ReceiveMessage(CancellationToken ct)
        {
            return _channel.ReceiveMessage(ct);
        }

        protected override void OnAgreementReceived(Message message)
        {
            //nothing
        }

        protected override void OnKeepAlivedReceived(Message message)
        {
            //nothing
        }

        protected override Task OnClose(CancellationToken ct)
        {
            return _channel.Close(ct);
        }
    }
}
