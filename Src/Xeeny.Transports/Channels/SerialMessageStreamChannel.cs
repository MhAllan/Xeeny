using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports.Channels
{
    public class SerialMessageStreamChannel : IMessageChannel
    {
        const byte _sizeIndex = 0; //4 bytes int
        const byte _messageTypeIndex = 4; //1 byte flag
        const byte _idIndex = 5; //16 bytes guid
        const byte _payloadIndex = 21;

        public ConnectionSide ConnectionSide => _transportChannel.ConnectionSide;
        public string ConnectionName => _transportChannel.ConnectionName;

        readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        readonly ITransportChannel _transportChannel;
        readonly int _minMessageSize = _payloadIndex;
        readonly int _maxMessageSize;
        readonly int _receiveBufferSize;
        readonly int _sendBufferSize;

        public Task Connect(CancellationToken ct)
        {
            return _transportChannel.Connect(ct);
        }

        public SerialMessageStreamChannel(ITransportChannel channel, TransportSettings settings)
        {
            var maxMessageSize = settings.MaxMessageSize;
            var sendBufferSize = settings.SendBufferSize;
            var receiveBufferSize = settings.ReceiveBufferSize;

            if (maxMessageSize <= _minMessageSize)
            {
                throw new Exception($"settings property {nameof(settings.MaxMessageSize)} must be larger " +
                    $"then {_minMessageSize}");
            }
            if (sendBufferSize <= _minMessageSize)
            {
                throw new Exception($"settings property {nameof(settings.SendBufferSize)} must be larger " +
                    $"then {_minMessageSize}");
            }
            if (receiveBufferSize <= _minMessageSize)
            {
                throw new Exception($"settings property {nameof(settings.ReceiveBufferSize)} must be larger " +
                    $"then {_minMessageSize}");
            }

            _transportChannel = channel;
            _maxMessageSize = maxMessageSize;
            _sendBufferSize = sendBufferSize;
            _receiveBufferSize = receiveBufferSize;
            _maxMessageSize = settings.MaxMessageSize;
            _receiveBufferSize = settings.ReceiveBufferSize;
            _sendBufferSize = settings.SendBufferSize;
        }

        public async Task SendMessage(Message message, CancellationToken ct)
        {
            var msgSize = _minMessageSize;
            var payloadSize = 0;
            var payload = message.Payload;
            if (payload != null)
            {
                payloadSize = payload.Length;
                msgSize += payloadSize;
            }

            var buffer = ArrayPool<byte>.Shared.Rent(msgSize);
            var locked = false;
            try
            {
                BufferHelper.CopyToIndex(BitConverter.GetBytes(msgSize), buffer, _sizeIndex);
                buffer[_messageTypeIndex] = (byte)message.MessageType;
                BufferHelper.CopyToIndex(message.Id.ToByteArray(), buffer, _idIndex);
                if (payloadSize > 0)
                {
                    BufferHelper.CopyToIndex(payload, buffer, _payloadIndex);
                }

                var offset = 0;
                await _sendLock.WaitAsync();
                locked = true;

                while (!ct.IsCancellationRequested && offset < msgSize)
                {
                    var left = msgSize - offset;
                    var next = Math.Min(_sendBufferSize, left);
                    var segment = new ArraySegment<byte>(buffer, offset, next);
                    await _transportChannel.SendAsync(segment, ct);

                    offset += next;
                }
            }
            finally
            {
                if (locked)
                {
                    _sendLock.Release();
                }

                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async Task<Message> ReceiveMessage(CancellationToken ct)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(_receiveBufferSize);
            try
            {
                var read = 0;
                var msgSize = -1;

                while (msgSize == -1 || read < msgSize)
                {
                    var len = msgSize == -1 ? 4 : msgSize - read;
                    var segment = new ArraySegment<byte>(buffer, read, len);

                    read += await _transportChannel.ReceiveAsync(segment, ct);
                    if (msgSize == -1 && read >= 4)
                    {
                        msgSize = BitConverter.ToInt32(buffer, 0);
                        if (msgSize < _minMessageSize)
                        {
                            throw new Exception($"Received message size {msgSize} while minimum is {_minMessageSize}");
                        }
                        if (msgSize > _maxMessageSize)
                        {
                            throw new Exception($"Received message size {msgSize} while maximum is {_maxMessageSize}");
                        }
                        if (msgSize > _receiveBufferSize)
                        {
                            var newBuffer = ArrayPool<byte>.Shared.Rent(msgSize);
                            Buffer.BlockCopy(buffer, 0, newBuffer, 0, read);
                            ArrayPool<byte>.Shared.Return(buffer);
                            buffer = newBuffer;
                        }
                    }

                    ct.ThrowIfCancellationRequested();
                }

                var msgType = (MessageType)buffer[_messageTypeIndex];
                var id = new Guid(BufferHelper.GetSubArray(buffer, _idIndex, 16));
                byte[] payload = null;
                var payloadLength = msgSize - _payloadIndex;
                if (payloadLength > 0)
                {
                    payload = BufferHelper.GetSubArray(buffer, _payloadIndex, payloadLength);
                }

                return new Message(msgType, id, payload);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async Task Close(CancellationToken ct)
        {
            await _transportChannel.Close(ct);
        }
    }
}
