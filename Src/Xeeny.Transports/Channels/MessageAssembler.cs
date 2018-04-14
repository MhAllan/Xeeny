using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xeeny.Transports.Channels
{
    class MessageAssembler : IDisposable
    {
        public Guid MessageId => _messageId;
        public bool IsComplete => _isComplete;
        public DateTime CreationTime => _creationTime;

        readonly Guid _messageId;
        readonly int _totalSize;
        readonly DateTime _creationTime;

        int _currentSize;
        byte[] _buffer;
        bool _isComplete;
        List<int> _receivedIndexes;

        public MessageAssembler(Guid messageId, int totalSize)
        {
            _messageId = messageId;
            _totalSize = totalSize;
            _buffer = ArrayPool<byte>.Shared.Rent(totalSize);
            _isComplete = false;
            _currentSize = 0;
            _receivedIndexes = new List<int>();
            _creationTime = DateTime.Now;
        }

        public bool AddPartialMessage(ArraySegment<byte> partialMessage, int index)
        {
            if(index < 0)
            {
                throw new ArgumentException(nameof(index));
            }
            var count = partialMessage.Count;
            if(count - index > _totalSize)
            {
                throw new Exception($"Too big partial message, count: {count}, index {index}" +
                    $" while available is {_totalSize - index}");
            }
            BufferHelper.CopyToIndex(partialMessage.Array, _buffer, index);
            if (!_receivedIndexes.Any(x => x == index))
            {
                _receivedIndexes.Add(index);
                _currentSize += count;
            }
            _isComplete = _currentSize == _totalSize;
            return _isComplete;
        }

        public ArraySegment<byte> GetMessage()
        {
            if(!IsComplete)
            {
                throw new Exception("Message is not complete");
            }
            return new ArraySegment<byte>(_buffer, 0, _totalSize);
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = null;
            _receivedIndexes = null;
        }
    }
}
