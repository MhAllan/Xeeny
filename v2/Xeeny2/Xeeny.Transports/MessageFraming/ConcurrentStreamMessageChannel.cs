﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports.Channels;

namespace Xeeny.Transports.MessageFraming
{
    public class ConcurrentStreamMessageChannel : MessageChannel
    {
        const byte _msgHeader = 4 + 16 + 4; //size + message id + total size
        const byte _minMsgSize = _msgHeader + 1;

        internal static byte HeaderSize => _msgHeader;

        readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        
        readonly int _maxMessageSize;
        readonly int _receiveBufferSize;
        readonly int _sendBufferSize;
        readonly int _availableSize;

        readonly long _receiveTimeoutMS;
        Dictionary<Guid, StreamMessageAssembler> _assemblers = new Dictionary<Guid, StreamMessageAssembler>();
        readonly Timer _assemblersCleanTimer;

        public ConcurrentStreamMessageChannel(Channel channel, ConcurrentStreamChannelSettings settings)
            : base(channel)
        {
            if (settings.MaxMessageSize < _msgHeader)
            {
                throw new Exception($"{nameof(settings.MaxMessageSize)} must be larger than {_msgHeader}");
            }
            if (settings.SendBufferSize < _msgHeader)
            {
                throw new Exception($"{nameof(settings.SendBufferSize)} must be larger than {_msgHeader}");
            }
            if (settings.ReceiveBufferSize < _msgHeader)
            {
                throw new Exception($"{nameof(settings.ReceiveBufferSize)} must be larger than {_msgHeader}");
            }
            if (settings.MaxMessageSize < settings.SendBufferSize)
            {
                throw new Exception($"{nameof(settings.MaxMessageSize)} must be larger than {settings.SendBufferSize}");
            }
            if (settings.MaxMessageSize < settings.ReceiveBufferSize)
            {
                throw new Exception($"{nameof(settings.MaxMessageSize)} must be larger than {settings.ReceiveBufferSize}");
            }

            _maxMessageSize = settings.MaxMessageSize;
            _sendBufferSize = settings.SendBufferSize;
            _receiveBufferSize = settings.ReceiveBufferSize;

            _availableSize = _sendBufferSize - _msgHeader;

            _receiveTimeoutMS = settings.ReceiveTimeout.TotalMilliseconds;
            _assemblersCleanTimer = new Timer(CleanAssemblers, null, 0, _receiveTimeoutMS);
        }

        public override async Task SendMessage(byte[] message, CancellationToken ct)
        {
            if (message == null || message.Length == 0)
                throw new ArgumentException(nameof(message));

            var msgSize = message.Length;
            var msgId = Guid.NewGuid();
            var msgIndex = 0;

            var buffer = ArrayPool<byte>.Shared.Rent(_sendBufferSize);
            try
            {
                while(!ct.IsCancellationRequested && msgIndex < msgSize)
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
                        await SendBytes(segment, ct);
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

        public override async Task<byte[]> ReceiveMessage(CancellationToken ct)
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
                    var msgId = Guid.Empty;

                    var index = 0;

                    while (partialMsgSize == -1 || read < partialMsgSize)
                    {
                        var len = partialMsgSize == -1 ? _msgHeader : partialMsgSize - read;
                        var segment = new ArraySegment<byte>(buffer, read, len);

                        read += await ReceiveBytes(segment, ct);

                        if (partialMsgSize == -1 && read >= _msgHeader)
                        {
                            index = buffer.ReadInt32(0, out partialMsgSize);
                            index = buffer.ReadGuid(index, out msgId);
                            index = buffer.ReadInt32(index, out msgSize);

                            if (msgSize > _maxMessageSize)
                            {
                                throw new Exception($"Received message size {partialMsgSize} " +
                                    $"while maximum is {_maxMessageSize}");
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

        public override async Task Close(CancellationToken ct)
        {
            await base.Close(ct);

            _assemblersCleanTimer.Dispose();
            foreach (var assembler in _assemblers.Values)
            {
                DisposeAssembler(assembler);
            }
            _assemblers = null;
        }
    }
}
