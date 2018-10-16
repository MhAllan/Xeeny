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
    public class SerialStreamTransport : MessageTransport
    {
        public override int MinMessageSize => _minMessageSize;

        const int _minMessageSize = 5; //4 header (msg size) and at least one payload

        readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public SerialStreamTransport(TransportChannel channel, MessageTransportSettings settings, ILoggerFactory loggerFactory)
            : base(channel, settings, loggerFactory)
        {

        }

        protected override async Task SendMessage(byte[] message, CancellationToken ct)
        {
            if (message == null || message.Length == 0)
                throw new ArgumentException(nameof(message));

            byte[] buffer = null;
            await _lock.WaitAsync();
            try
            {
                var msgSize = message.Length;
                var all = msgSize + 4;
                var sendSize = Math.Min(SendBufferSize, all);
                var take = sendSize - 4;

                buffer = ArrayPool<byte>.Shared.Rent(sendSize);
                var index = buffer.WriteInt32(0, msgSize);
                index = buffer.WriteArray(4, message, take);

                var segment = new ArraySegment<byte>(buffer, 0, index);
                await Channel.Send(segment, ct);

                ArrayPool<byte>.Shared.Return(buffer);
                buffer = null;

                index = take;

                while (index < msgSize)
                {
                    sendSize = Math.Min(SendBufferSize, msgSize - index);
                    segment = new ArraySegment<byte>(message, index, sendSize);
                    await Channel.Send(segment, ct);

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

        protected override async Task<byte[]> ReceiveMessage(CancellationToken ct)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);
            try
            {
                var msgSize = -1;
                var read = 0;

                while (msgSize == -1 || read < msgSize)
                {
                    var len = msgSize == -1 ? 4 : msgSize - read;
                    var segment = new ArraySegment<byte>(buffer, read, len);

                    read += await Channel.Receive(segment, ct);

                    if (msgSize == -1 && read >= 4)
                    {
                        msgSize = BitConverter.ToInt32(buffer, 0);
                        read -= 4;

                        if (msgSize > MaxMessageSize)
                        {
                            throw new Exception($"Received message size {msgSize} while maximum is {MaxMessageSize}");
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
