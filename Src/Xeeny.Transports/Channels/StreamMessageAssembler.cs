using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xeeny.Transports.Channels
{
    class StreamMessageAssembler : IDisposable
    {
        public Guid MessageId => _messageId;
        public bool IsComplete => _isComplete;
        public DateTime CreationTime => _creationTime;

        readonly Guid _messageId;
        readonly int _totalSize;
        readonly DateTime _creationTime;

        byte[] _buffer;
        bool _isComplete;
        int _index;

        public StreamMessageAssembler(Guid messageId, int totalSize)
        {
            _messageId = messageId;
            _totalSize = totalSize;
            _buffer = ArrayPool<byte>.Shared.Rent(totalSize);
            _isComplete = false;
            _creationTime = DateTime.Now;
        }

        public bool AddPartialMessage(ArraySegment<byte> partialMessage)
        {
            if(IsComplete)
            {
                throw new Exception("Message is already complete");
            }

            var count = partialMessage.Count;
            if(count + _index > _totalSize)
            {
                throw new Exception($"Too big partial message, count: {count}, index {_index}" +
                    $" while available is {_totalSize - _index}");
            }
            Buffer.BlockCopy(partialMessage.Array, partialMessage.Offset, _buffer, _index, partialMessage.Count);
            _index += count;
            _isComplete = _index == _totalSize;
            return _isComplete;
        }

        public byte[] GetMessage()
        {
            if(!IsComplete)
            {
                throw new Exception("Message is not complete");
            }
            return _buffer;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = null;
        }
    }
}
