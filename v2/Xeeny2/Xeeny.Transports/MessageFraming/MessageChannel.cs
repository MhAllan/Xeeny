using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports.Channels;
using Xeeny.Transports.Messages;

namespace Xeeny.Transports.MessageFraming
{
    public abstract class MessageChannel
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

        public ConnectionSide Connectionside { get; }

        readonly Channel _channel;
        public MessageChannel(Channel channel)
        {
            _channel = channel;
            Connectionside = channel.ConnectionSide;
        }

        public virtual Task Connect(CancellationToken ct)
        {
            return _channel.Connect(ct);
        }

        public virtual Task Close(CancellationToken ct)
        {
            return _channel.Close(ct);
        }

        public abstract Task SendMessage(byte[] message, CancellationToken ct);
        public abstract Task<byte[]> ReceiveMessage(CancellationToken ct);

        protected virtual Task SendBytes(ArraySegment<byte> buffer, CancellationToken ct)
        {
            return _channel.Send(buffer, ct);
        }

        protected virtual Task<int> ReceiveBytes(ArraySegment<byte> buffer, CancellationToken ct)
        {
            return _channel.Receive(buffer, ct);
        }
    }
}
