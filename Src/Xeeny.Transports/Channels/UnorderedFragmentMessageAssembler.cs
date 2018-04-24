using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xeeny.Transports.Channels
{
    class UnorderedFragmentMessageAssembler : IDisposable
    {
        public Guid MessageId => _messageId;
        public bool IsComplete => _isComplete;
        public DateTime CreationTime => _creationTime;

        readonly Guid _messageId;
        readonly int _totalSize;
        readonly DateTime _creationTime;

        byte[] _buffer;
        bool _isComplete;
        List<int> _servedIndexes = new List<int>();
        int _count;

        public UnorderedFragmentMessageAssembler(Guid messageId, int totalSize)
        {
            _messageId = messageId;
            _totalSize = totalSize;
            _buffer = ArrayPool<byte>.Shared.Rent(totalSize);
            _isComplete = false;
            _creationTime = DateTime.Now;
        }

        public bool AddPartialMessage(int index, ArraySegment<byte> partialMessage)
        {
            if (IsComplete)
            {
                throw new Exception("Message is already complete");
            }

            if(!_servedIndexes.Any(x => x == index))
            {
                _count += partialMessage.Count;
                if (_count > _totalSize)
                {
                    throw new Exception($"Total message size is {_totalSize} while received {_count}");
                }

                Buffer.BlockCopy(partialMessage.Array, partialMessage.Offset, _buffer, index, partialMessage.Count);
                _servedIndexes.Add(index);
            }

            _isComplete = _count == _totalSize;
            return _isComplete;
        }

        public byte[] GetMessage()
        {
            if (!IsComplete)
            {
                throw new Exception("Message is not complete");
            }
            return _buffer;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = null;
            _servedIndexes = null;
        }
    }
}
