using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports.Channels;
using Xeeny.Transports.Messages;

namespace Xeeny.Transports.MessageChannels
{
    public class SerialStreamMessageChannel : MessageChannel
    {
        const int _minMessageSize = 5;

        readonly int _maxMessageSize;
        readonly int _sendBufferSize;
        readonly int _receiveBufferSize;

        readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public SerialStreamMessageChannel(Channel channel, SerialStreamChannelSettings settings)
            : base(channel)
        {
            if (settings.MaxMessageSize <= _minMessageSize)
            {
                throw new Exception($"{nameof(settings.MaxMessageSize)} must be larger than {_minMessageSize}");
            }
            if (settings.SendBufferSize <= _minMessageSize)
            {
                throw new Exception($"{nameof(settings.SendBufferSize)} must be larger than {_minMessageSize}");
            }
            if (settings.ReceiveBufferSize <= _minMessageSize)
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

        public override async Task Send(Message message, CancellationToken ct)
        {
            var data = message.GetData();
            var count = data.Length;
            if (count == 0)
                return;

            await _lock.WaitAsync();
            try
            {
                var all = count + 4;
                var sendSize = Math.Min(_sendBufferSize, all);
                var array = new byte[sendSize];
                var arrIndex = array.WriteInt32(0, count);
                var take = sendSize - arrIndex;
                array.WriteArray(arrIndex, data, take);

                var segment = new ArraySegment<byte>(array);

                await SendBytes(segment, ct);

                var sentCount = sendSize - 4;

                while (!ct.IsCancellationRequested && sentCount < count)
                {
                    sendSize = Math.Min(_sendBufferSize, count - sentCount);
                    segment = new ArraySegment<byte>(data, sentCount, sendSize);
                    await SendBytes(segment, ct);
                    sentCount += segment.Count;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public override async Task<Message> Receive(CancellationToken ct)
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

                return new Message(buffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
