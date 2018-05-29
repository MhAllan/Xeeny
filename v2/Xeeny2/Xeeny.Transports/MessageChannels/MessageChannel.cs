using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports.Channels;
using Xeeny.Transports.Messages;

namespace Xeeny.Transports.MessageChannels
{
    public abstract class MessageChannel : IConnectionObject
    {
        public string ConnectionId
        {
            get => _channel.ConnectionId;
            internal set => _channel.ConnectionId = value;
        }

        public string ConnectionName
        {
            get => _channel.ConnectionName;
            internal set => _channel.ConnectionName = value;
        }

        public ConnectionSide ConnectionSide => _channel.ConnectionSide;

        readonly Channel _channel;

        public MessageChannel(Channel channel)
        {
            _channel = channel;
        }

        public Task Connect(CancellationToken ct)
        {
            return _channel.Connect(ct);
        }

        public abstract Task Send(Message message, CancellationToken ct);
        public abstract Task<Message> Receive(CancellationToken ct);

        protected Task SendBytes(ArraySegment<byte> data, CancellationToken ct)
        {
            return _channel.Send(data, ct);
        }

        protected Task<int> ReceiveBytes(ArraySegment<byte> data, CancellationToken ct)
        {
            return _channel.Receive(data, ct);
        }

        public Task Close(CancellationToken ct)
        {
            return _channel.Close(ct);
        }
    }
}
