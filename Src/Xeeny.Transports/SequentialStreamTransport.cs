using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports.Buffers;

namespace Xeeny.Transports
{
    public abstract class SequentialStreamTransport : TransportBase
    {
        const byte _sizeIndex = 0; //4 bytes ing
        const byte _messageTypeIndex = 4; //1 byte flag
        const byte _idIndex = 5; //16 bytes guid
        const byte _payloadIndex = 21;

        protected override int MinMessageSize => _payloadIndex;

        readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        RichBuffer _buffer;
        int _size;

        public SequentialStreamTransport(TransportSettings settings, ILogger logger) : base(settings, logger)
        {
            _buffer = new RichBuffer(settings.ReceiveBufferSize);
        }

        protected abstract Task Send(byte[] sendBuffer, int count, CancellationToken ct);
        protected abstract Task<int> Receive(byte[] receiveBuffer, CancellationToken ct);

        protected override async Task<Message> ReceiveMessage(byte[] receiveBuffer, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && (_size == 0 || _size > _buffer.CurrentSize))
            {
                var read = await Receive(receiveBuffer, ct);
                _buffer.Write(receiveBuffer, 0, read);
                _size = GetNextMessageSize();
            }

            var messageType = (MessageType)_buffer[_messageTypeIndex];
            var id = new Guid(_buffer.Read(_idIndex, 16));
            var payloadSize = _size - _payloadIndex;
            byte[] payload = null;
            if (payloadSize > 0)
            {
                payload = _buffer.Read(_payloadIndex, payloadSize);
            }

            var msg = new Message(messageType, id, payload);

            _buffer.Trim(_size);
            _size = GetNextMessageSize();

            return msg;
        }

        int GetNextMessageSize()
        {
            if (_buffer.TryReadInteger(0, out int size))
            {
                if (size > MaxMessageSize)
                {
                    throw new Exception($"Received message size is {size} while maximum is {MaxMessageSize}");
                }
            }
            return size;
        }

        protected override async Task SendMessage(Message message, byte[] sendBuffer, CancellationToken ct)
        {
            await _sendLock.WaitAsync();
            try
            {
                var size = MinMessageSize;
                var payloadSize = 0;
                var payload = message.Payload;
                if (payload != null)
                {
                    payloadSize = payload.Length;
                    size += payloadSize;
                }

                BufferHelper.CopyToIndex(BitConverter.GetBytes(size), sendBuffer, _sizeIndex);
                sendBuffer[_messageTypeIndex] = (byte)message.MessageType;
                BufferHelper.CopyToIndex(message.Id.ToByteArray(), sendBuffer, _idIndex);
                var bufferIndex = _payloadIndex;
                var payloadFragmentIndex = 0;

                do
                {
                    int count = 0;
                    if (payloadSize > 0)
                    {
                        count = Math.Min(payloadSize - payloadFragmentIndex, sendBuffer.Length - bufferIndex);
                        Buffer.BlockCopy(payload, payloadFragmentIndex, sendBuffer, bufferIndex, count);
                    }

                    await Send(sendBuffer, count + bufferIndex, ct);

                    payloadFragmentIndex += count;
                    bufferIndex = 0;

                } while (payloadFragmentIndex < payloadSize);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        protected override void OnClose(CancellationToken ct)
        {
            _buffer.Dispose();
        }
    }
}
