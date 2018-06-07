using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports.Channels;
using Xeeny.Transports.Messages;

namespace Xeeny.Transports.MessageFraming
{
    public class SerialStreamMessageChannel : MessageChannel
    {
        const int _minMessageSize = 5;

        readonly int _maxMessageSize;
        readonly int _sendBufferSize;
        readonly int _receiveBufferSize;

        readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public SerialStreamMessageChannel(Channel next, SerialStreamChannelSettings settings)
            : base(next)
        {
            if (settings.MaxMessageSize < _minMessageSize)
            {
                throw new Exception($"{nameof(settings.MaxMessageSize)} must be larger than {_minMessageSize}");
            }
            if (settings.SendBufferSize < _minMessageSize)
            {
                throw new Exception($"{nameof(settings.SendBufferSize)} must be larger than {_minMessageSize}");
            }
            if (settings.ReceiveBufferSize < _minMessageSize)
            {
                throw new Exception($"{nameof(settings.ReceiveBufferSize)} must be larger than {_minMessageSize}");
            }
            if(settings.MaxMessageSize < settings.SendBufferSize)
            {
                throw new Exception($"{nameof(settings.MaxMessageSize)} must be larger than {settings.SendBufferSize}");
            }
            if(settings.MaxMessageSize < settings.ReceiveBufferSize)
            {
                throw new Exception($"{nameof(settings.MaxMessageSize)} must be larger than {settings.ReceiveBufferSize}");
            }

            _maxMessageSize = settings.MaxMessageSize;
            _receiveBufferSize = settings.ReceiveBufferSize;
            _sendBufferSize = settings.SendBufferSize;
        }

        public override async Task SendMessage(byte[] message, CancellationToken ct)
        {
            if (message == null || message.Length == 0)
                throw new ArgumentException(nameof(message));

            byte[] buffer = null;
            await _lock.WaitAsync();
            try
            {
                var msgSize = message.Length;
                var all = msgSize + 4;
                var sendSize = Math.Min(_sendBufferSize, all);
                var take = sendSize - 4;

                buffer = ArrayPool<byte>.Shared.Rent(sendSize);
                var index = buffer.WriteInt32(0, msgSize);
                index = buffer.WriteArray(4, message, take);

                var segment = new ArraySegment<byte>(buffer, 0, index);
                await SendBytes(segment, ct);

                ArrayPool<byte>.Shared.Return(buffer);
                buffer = null;

                index = take;

                while(index < msgSize)
                {
                    sendSize = Math.Min(_sendBufferSize, msgSize - index);
                    segment = new ArraySegment<byte>(message, index, sendSize);
                    await SendBytes(segment, ct);

                    index += sendSize;
                }

            }
            finally
            {
                _lock.Release();
                if (buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        public override async Task<byte[]> ReceiveMessage(CancellationToken ct)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(_receiveBufferSize);
            try
            {
                var msgSize = -1;
                var read = 0;

                while (msgSize == -1 || read < msgSize)
                {
                    var len = msgSize == -1 ? 4 : msgSize - read;
                    var segment = new ArraySegment<byte>(buffer, read, len);

                    read += await ReceiveBytes(segment, ct);

                    if (msgSize == -1 && read >= 4)
                    {
                        msgSize = BitConverter.ToInt32(buffer, 0);
                        read -= 4;

                        if (msgSize > _maxMessageSize)
                        {
                            throw new Exception($"Received message size {msgSize} while maximum is {_maxMessageSize}");
                        }
                        if (msgSize > buffer.Length)
                        {
                            var newBuffer = ArrayPool<byte>.Shared.Rent(msgSize);
                            Buffer.BlockCopy(buffer, 0, newBuffer, 0, read);
                            ArrayPool<byte>.Shared.Return(buffer);
                            buffer = newBuffer;
                        }
                    }
                }

                ct.ThrowIfCancellationRequested();

                return buffer.GetSubArray(0, msgSize);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
