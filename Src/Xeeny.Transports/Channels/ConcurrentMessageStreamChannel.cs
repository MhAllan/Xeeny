using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports.Channels
{
    public class ConcurrentMessageStreamChannel : IMessageChannel
    {
        const byte _totalSizeIndex = 0; //4 bytes int
        const byte _sizeIndex = 4; //4 bytes int
        const byte _messageTypeIndex = 8; //1 byte flag
        const byte _idIndex = 9; //16 bytes guid
        const byte _payloadIndex = 25;

        public ConnectionSide ConnectionSide => _transportChannel.ConnectionSide;
        public string ConnectionName => _transportChannel.ConnectionName;

        readonly ITransportChannel _transportChannel;
        readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        readonly int _minMessageSize = _payloadIndex;
        readonly int _maxMessageSize;
        readonly int _receiveBufferSize;
        readonly int _sendBufferSize;
        readonly int _availablePayloadSize;

        readonly MessageAssemblerManager _assemblerManager;

        public ConcurrentMessageStreamChannel(ITransportChannel channel, TransportSettings settings)
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
            _availablePayloadSize = _sendBufferSize - _minMessageSize;

            _assemblerManager = new MessageAssemblerManager(settings.ReceiveTimeout.TotalMilliseconds);
        }

        public Task Connect(CancellationToken ct)
        {
            return _transportChannel.Connect(ct);
        }

        public async Task SendMessage(Message message, CancellationToken ct)
        {
            var totalSize = _minMessageSize;
            var payloadSize = 0;
            var payload = message.Payload;
            if (payload != null)
            {
                payloadSize = payload.Length;
                totalSize += (int)Math.Ceiling((double)payloadSize / _availablePayloadSize);
            }

            var buffer = ArrayPool<byte>.Shared.Rent(_sendBufferSize);
            var payloadIndex = 0;
            try
            {
                do
                {
                    var partialPayloadSize = Math.Min(payloadSize - payloadIndex, _availablePayloadSize);
                    var msgSize = _minMessageSize + partialPayloadSize;

                    BufferHelper.CopyToIndex(BitConverter.GetBytes(totalSize), buffer, _totalSizeIndex);
                    BufferHelper.CopyToIndex(BitConverter.GetBytes(msgSize), buffer, _sizeIndex);
                    buffer[_messageTypeIndex] = (byte)message.MessageType;
                    BufferHelper.CopyToIndex(message.Id.ToByteArray(), buffer, _idIndex);
                    if (partialPayloadSize > 0)
                    {
                        Buffer.BlockCopy(payload, payloadIndex, buffer, _payloadIndex, partialPayloadSize);
                    }
                    var segment = new ArraySegment<byte>(buffer, 0, msgSize);
                    await _sendLock.WaitAsync();
                    try
                    {
                        await _transportChannel.SendAsync(segment, ct);
                    }
                    finally
                    {
                        _sendLock.Release();
                    }
                    payloadIndex += partialPayloadSize;
                }
                while (!ct.IsCancellationRequested && payloadIndex < payloadSize);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async Task<Message> ReceiveMessage(CancellationToken ct)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(_receiveBufferSize);
            try
            {
                while(!ct.IsCancellationRequested)
                {
                    var read = 0;
                    var msgSize = -1;

                    while (!ct.IsCancellationRequested && (msgSize == -1 || read < msgSize))
                    {
                        var len = msgSize == -1 ? 4 : msgSize - read;
                        var segment = new ArraySegment<byte>(buffer, read, len);

                        read += await _transportChannel.ReceiveAsync(segment, ct);
                        if (msgSize == -1 && read >= 8)
                        {
                            msgSize = BitConverter.ToInt32(buffer, _sizeIndex);
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

                    }

                    ct.ThrowIfCancellationRequested();

                    var totalMessageSize = BitConverter.ToInt32(buffer, _totalSizeIndex);
                    var id = new Guid(BufferHelper.GetSubArray(buffer, _idIndex, 16));

                    if (totalMessageSize <= _receiveBufferSize)
                    {
                        return GetMessageFromBuffer(buffer, totalMessageSize, id);
                    }
                    else
                    {
                        var segment = new ArraySegment<byte>(buffer, 0, msgSize);
                        if (_assemblerManager.AddPartialAndTryGetMessage(totalMessageSize, id, 0, segment, out var result))
                        {
                            return GetMessageFromBuffer(result.Array, totalMessageSize, id);
                        }
                    }
                }
                throw new TaskCanceledException();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        Message GetMessageFromBuffer(byte[] buffer, int msgSize, Guid id)
        {
            var msgType = (MessageType)buffer[_messageTypeIndex];
            byte[] payload = null;
            var payloadLength = msgSize - _payloadIndex;
            if (payloadLength > 0)
            {
                payload = BufferHelper.GetSubArray(buffer, _payloadIndex, payloadLength);
            }

            return new Message(msgType, id, payload);
        }

        public void Close(CancellationToken ct)
        {
            _assemblerManager.Dispose();
            _transportChannel.Close(ct);
        }
    }
}
