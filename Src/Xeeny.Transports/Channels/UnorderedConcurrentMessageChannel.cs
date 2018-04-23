using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports.Channels
{
    public class UnorderedConcurrentMessageChannel : IMessageChannel
    {
        const byte _partialMsgSizeIndex = 0; //4 bytes int
        const byte _msgSizeIndex = 4; //4 bytes int
        const byte _partialMsgTypeIndex = 8; //1 byte flag
        const byte _msgIdIndex = 9; //16 bytes guid
        const byte _fragmentIdIndex = 25; //4 bytes int
        const byte _partialPayloadIndex = 29;
        const byte _msgTypeIndex = _partialMsgTypeIndex - _msgSizeIndex;
        const byte _payloadIndex = _partialPayloadIndex - _msgSizeIndex;

        public ConnectionSide ConnectionSide => _transportChannel.ConnectionSide;
        public string ConnectionName => _transportChannel.ConnectionName;

        readonly ITransportChannel _transportChannel;
        readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        readonly byte _minPartialMsgSize = _partialPayloadIndex;
        readonly byte _minMsgSize = _payloadIndex;
        readonly int _maxMessageSize;
        readonly int _receiveBufferSize;
        readonly int _sendBufferSize;
        readonly int _availablePayloadSize;

        readonly long _receiveTimeoutMS;
        ConcurrentDictionary<Guid, UnorderedFragmentMessageAssembler> _assemblers =
            new ConcurrentDictionary<Guid, UnorderedFragmentMessageAssembler>();
        readonly Timer _assemblersCleanTimer;

        public UnorderedConcurrentMessageChannel(ITransportChannel channel, TransportSettings settings)
        {
            var maxMessageSize = settings.MaxMessageSize;
            var sendBufferSize = settings.SendBufferSize;
            var receiveBufferSize = settings.ReceiveBufferSize;

            if (maxMessageSize <= _minPartialMsgSize)
            {
                throw new Exception($"settings property {nameof(settings.MaxMessageSize)} must be larger " +
                    $"then {_minPartialMsgSize}");
            }
            if (sendBufferSize <= _minPartialMsgSize)
            {
                throw new Exception($"settings property {nameof(settings.SendBufferSize)} must be larger " +
                    $"then {_minPartialMsgSize}");
            }
            if (receiveBufferSize <= _minPartialMsgSize)
            {
                throw new Exception($"settings property {nameof(settings.ReceiveBufferSize)} must be larger " +
                    $"then {_minPartialMsgSize}");
            }

            _transportChannel = channel;
            _maxMessageSize = maxMessageSize;
            _sendBufferSize = sendBufferSize;
            _receiveBufferSize = receiveBufferSize;
            _maxMessageSize = settings.MaxMessageSize;
            _receiveBufferSize = settings.ReceiveBufferSize;
            _sendBufferSize = settings.SendBufferSize;
            _availablePayloadSize = _sendBufferSize - _minPartialMsgSize;

            _receiveTimeoutMS = settings.ReceiveTimeout.TotalMilliseconds;
            _assemblersCleanTimer = new Timer(CleanAssemblers, null, 0, _receiveTimeoutMS);
        }

        public Task Connect(CancellationToken ct)
        {
            return _transportChannel.Connect(ct);
        }

        public async Task SendMessage(Message message, CancellationToken ct)
        {
            int msgSize = _minMsgSize;
            var payloadSize = 0;
            var payload = message.Payload;
            if (payload != null)
            {
                payloadSize = payload.Length;
                msgSize += payloadSize;
            }

            var buffer = ArrayPool<byte>.Shared.Rent(_sendBufferSize);
            var payloadIndex = 0;
            try
            {
                do
                {
                    var partialPayloadSize = Math.Min(payloadSize - payloadIndex, _availablePayloadSize);
                    var partialMsgSize = _minPartialMsgSize + partialPayloadSize;

                    BufferHelper.CopyToIndex(BitConverter.GetBytes(partialMsgSize), buffer, _partialMsgSizeIndex);
                    BufferHelper.CopyToIndex(BitConverter.GetBytes(msgSize), buffer, _msgSizeIndex);
                    BufferHelper.CopyToIndex(BitConverter.GetBytes(payloadIndex), buffer, _fragmentIdIndex);
                    buffer[_partialMsgTypeIndex] = (byte)message.MessageType;
                    BufferHelper.CopyToIndex(message.Id.ToByteArray(), buffer, _msgIdIndex);
                    if (partialPayloadSize > 0)
                    {
                        Buffer.BlockCopy(payload, payloadIndex, buffer, _partialPayloadIndex, partialPayloadSize);
                    }
                    var segment = new ArraySegment<byte>(buffer, 0, partialMsgSize);
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
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    var read = 0;
                    var partialMsgSize = -1;
                    var msgSize = 0;
                    while (partialMsgSize == -1 || read < partialMsgSize)
                    {
                        var len = partialMsgSize == -1 ? 8 : partialMsgSize - read;
                        var segment = new ArraySegment<byte>(buffer, read, len);

                        read += await _transportChannel.ReceiveAsync(segment, ct);
                        if (partialMsgSize == -1 && read >= 8)
                        {
                            partialMsgSize = BitConverter.ToInt32(buffer, _partialMsgSizeIndex);
                            msgSize = BitConverter.ToInt32(buffer, _msgSizeIndex);
                            if (partialMsgSize < _minPartialMsgSize)
                            {
                                throw new Exception($"Received message size {partialMsgSize} while minimum is {_minPartialMsgSize}");
                            }
                            if (partialMsgSize > _receiveBufferSize)
                            {
                                var newBuffer = ArrayPool<byte>.Shared.Rent(partialMsgSize);
                                Buffer.BlockCopy(buffer, 0, newBuffer, 0, read);
                                ArrayPool<byte>.Shared.Return(buffer);
                                buffer = newBuffer;
                            }
                            if (msgSize > _maxMessageSize)
                            {
                                throw new Exception($"Received message size {partialMsgSize} while maximum is {_maxMessageSize}");
                            }
                        }

                        ct.ThrowIfCancellationRequested();

                    }

                    var msgId = new Guid(BufferHelper.GetSubArray(buffer, _msgIdIndex, 16));
                    if (msgSize == partialMsgSize - _msgSizeIndex)
                    {
                        var msgType = (MessageType)buffer[_partialMsgTypeIndex];
                        byte[] payload = null;
                        var payloadLength = partialMsgSize - _partialPayloadIndex;
                        if (payloadLength > 0)
                        {
                            payload = BufferHelper.GetSubArray(buffer, _partialPayloadIndex, payloadLength);
                        }

                        return new Message(msgType, msgId, payload);
                    }
                    else
                    {
                        var nextIndex = _msgSizeIndex;
                        if (!_assemblers.TryGetValue(msgId, out var assembler))
                        {
                            assembler = new UnorderedFragmentMessageAssembler(msgId, msgSize);
                            _assemblers.TryAdd(msgId, assembler);
                        }
                        else
                        {
                            nextIndex = _partialPayloadIndex;
                        }
                        var nextSegment = new ArraySegment<byte>(buffer, nextIndex, partialMsgSize - nextIndex);
                        var fragmentId = BitConverter.ToInt32(buffer, _fragmentIdIndex);
                        if (assembler.AddPartialMessage(fragmentId, nextSegment))
                        {
                            var msgBuffer = assembler.GetMessage();
                            var msgType = (MessageType)msgBuffer[_msgTypeIndex];
                            byte[] payload = null;
                            var payloadLength = msgSize - _payloadIndex;
                            if (payloadLength > 0)
                            {
                                payload = BufferHelper.GetSubArray(msgBuffer, _payloadIndex, payloadLength);
                            }

                            var msg = new Message(msgType, msgId, payload);
                            DisposeAssembler(assembler);
                            return msg;
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        void CleanAssemblers(object sender)
        {
            var now = DateTime.Now;
            foreach (var assembler in _assemblers.Values)
            {
                if ((now - assembler.CreationTime).TotalMilliseconds > _receiveTimeoutMS)
                {
                    DisposeAssembler(assembler);
                }
            }
        }

        void DisposeAssembler(UnorderedFragmentMessageAssembler assembler)
        {
            assembler.Dispose();
            _assemblers.TryRemove(assembler.MessageId, out var _);
        }

        public void Close(CancellationToken ct)
        {
            _assemblersCleanTimer.Dispose();
            foreach (var assembler in _assemblers.Values)
            {
                DisposeAssembler(assembler);
            }
            _assemblers = null;
            _transportChannel.Close(ct);
        }
    }
}
