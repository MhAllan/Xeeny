using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports.Channels;

namespace Xeeny.Transports
{
    public class ConcurrentStreamTransport : MessageTransport
    {
        public override int MinMessageSize => _minMsgSize;

        const byte _msgHeader = 4 + 16 + 4; //size + message id + total size
        const byte _minMsgSize = _msgHeader + 1;

        internal static byte HeaderSize => _msgHeader;

        readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        readonly int _availableSize;

        readonly long _receiveTimeoutMS;
        Dictionary<Guid, StreamMessageAssembler> _assemblers = new Dictionary<Guid, StreamMessageAssembler>();
        readonly Timer _assemblersCleanTimer;

        public ConcurrentStreamTransport(TransportChannel channel, MessageTransportSettings settings, ILoggerFactory loggerFactory)
            : base(channel, settings, loggerFactory)
        {
            _availableSize = SendBufferSize - _msgHeader;

            _receiveTimeoutMS = settings.ReceiveTimeout.TotalMilliseconds;
            _assemblersCleanTimer = new Timer(CleanAssemblers, null, 0, _receiveTimeoutMS);
        }

        protected override async Task SendMessage(byte[] message, CancellationToken ct)
        {
            if (message == null || message.Length == 0)
                throw new ArgumentException(nameof(message));

            var msgSize = message.Length;
            var msgId = Guid.NewGuid();
            var msgIndex = 0;

            var buffer = ArrayPool<byte>.Shared.Rent(SendBufferSize);
            try
            {
                while (!ct.IsCancellationRequested && msgIndex < msgSize)
                {
                    var take = Math.Min(msgSize - msgIndex, _availableSize);
                    var partialMsgSize = _msgHeader + take;

                    var index = buffer.WriteInt32(0, partialMsgSize);
                    index = buffer.WriteGuid(index, msgId);
                    index = buffer.WriteInt32(index, msgSize);
                    index = buffer.WriteArray(index, message, msgIndex, take);

                    var segment = new ArraySegment<byte>(buffer, 0, partialMsgSize);
                    await _sendLock.WaitAsync();
                    try
                    {
                        await Channel.Send(segment, ct);
                    }
                    finally
                    {
                        _sendLock.Release();
                    }
                    msgIndex += take;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        protected override async Task<byte[]> ReceiveMessage(CancellationToken ct)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);
            try
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    var read = 0;
                    var partialMsgSize = -1;
                    var msgSize = 0;
                    var msgId = Guid.Empty;

                    var index = 0;

                    while (partialMsgSize == -1 || read < partialMsgSize)
                    {
                        var len = partialMsgSize == -1 ? _msgHeader : partialMsgSize - read;
                        var segment = new ArraySegment<byte>(buffer, read, len);

                        read += await Channel.Receive(segment, ct);

                        if (partialMsgSize == -1 && read >= _msgHeader)
                        {
                            index = buffer.ReadInt32(0, out partialMsgSize);
                            index = buffer.ReadGuid(index, out msgId);
                            index = buffer.ReadInt32(index, out msgSize);

                            if (msgSize > MaxMessageSize)
                            {
                                throw new Exception($"Received message size {partialMsgSize} " +
                                    $"while maximum is {MaxMessageSize}");
                            }

                            if (partialMsgSize > buffer.Length)
                            {
                                var newBuffer = ArrayPool<byte>.Shared.Rent(partialMsgSize);
                                Buffer.BlockCopy(buffer, 0, newBuffer, 0, read);
                                ArrayPool<byte>.Shared.Return(buffer);
                                buffer = newBuffer;
                            }
                        }

                        ct.ThrowIfCancellationRequested();
                    }

                    var currentSize = partialMsgSize - _msgHeader;

                    if (msgSize == currentSize)
                    {
                        return buffer.GetSubArray(_msgHeader, msgSize);
                    }
                    else
                    {
                        if (!_assemblers.TryGetValue(msgId, out var assembler))
                        {
                            assembler = new StreamMessageAssembler(msgId, msgSize);
                            _assemblers.Add(msgId, assembler);
                        }

                        var nextSegment = new ArraySegment<byte>(buffer, _msgHeader, currentSize);

                        if (assembler.AddPartialMessage(nextSegment))
                        {
                            var msgBuffer = assembler.GetMessage();
                            var msg = msgBuffer.GetSubArray(0, msgSize);
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

        void DisposeAssembler(StreamMessageAssembler assembler)
        {
            assembler.Dispose();
            _assemblers.Remove(assembler.MessageId);
        }

        public override async Task Close(CancellationToken ct = default)
        {
            try
            {
                _assemblersCleanTimer.Dispose();
                foreach (var assembler in _assemblers.Values)
                {
                    DisposeAssembler(assembler);
                }
                _assemblers = null;
            }
            finally
            {
                await base.Close(ct);
            }
        }

        class StreamMessageAssembler : IDisposable
        {
            public readonly Guid MessageId;
            public readonly DateTime CreationTime;

            public bool IsComplete { get; private set; }

            readonly int _totalSize;
            byte[] _buffer;
            int _index;
            bool _isDisposed;

            public StreamMessageAssembler(Guid messageId, int totalSize)
            {
                MessageId = messageId;
                CreationTime = DateTime.Now;

                _totalSize = totalSize;

                _buffer = ArrayPool<byte>.Shared.Rent(totalSize);
            }

            public bool AddPartialMessage(ArraySegment<byte> segment)
            {
                if (_isDisposed)
                    throw new Exception("Assembler is disposed");

                if (IsComplete)
                {
                    throw new Exception("Message is already complete");
                }

                var count = segment.Count;

                if (count + _index > _totalSize)
                {
                    throw new Exception($"Too big partial message, count: {count}, index {_index}" +
                        $" while available is {_totalSize - _index}");
                }

                _index = _buffer.WriteSegment(_index, segment);

                var isComplete = _index == _totalSize;

                IsComplete = isComplete;

                return isComplete;
            }

            public byte[] GetMessage()
            {
                if (_isDisposed)
                    throw new Exception("Assembler is disposed");

                if (!IsComplete)
                {
                    throw new Exception("Message is not complete");
                }

                var result = _buffer.GetSubArray(0, _totalSize);

                Dispose();

                return result;
            }

            public void Dispose()
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
            }
        }
    }
}
