using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xeeny.Transports.MessageFraming
{
    class StreamMessageAssembler : IDisposable
    {
        public readonly Guid MessageId;
        public readonly DateTime CreationTime;

        public bool IsComplete { get; private set; }

        readonly int _totalSize;
        byte[] _buffer;
        int _index;

        public StreamMessageAssembler(Guid messageId, int totalSize)
        {
            MessageId = messageId;
            CreationTime = DateTime.Now;

            _totalSize = totalSize;

            _buffer = ArrayPool<byte>.Shared.Rent(totalSize);
        }

        public bool AddPartialMessage(ArraySegment<byte> segment)
        {
            if(IsComplete)
            {
                throw new Exception("Message is already complete");
            }

            var count = segment.Count;

            if(count + _index > _totalSize)
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
