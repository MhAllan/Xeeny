using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Xeeny.Sockets.Protocol.Messages;

namespace Xeeny.Sockets.Protocol.Formatters
{
    class FragmentCollection
    {
        public Guid MessageId => _messageId;

        public bool IsExpired => DateTime.UtcNow > _expirationDate;
        public bool IsCompleted { get; private set; }

        readonly MessageType _messageType;
        readonly Guid _messageId;
        readonly int _totalSize;
        readonly byte[][] _fragments;
        readonly DateTime _expirationDate;

        readonly int _fragmentsCount;
        int _currentFragmentsCount;

        public FragmentCollection(MessageType messageType, Guid messageId, int totalSize, int fragmentsCount, TimeSpan timeout)
        {
            _messageType = messageType;
            _messageId = messageId;
            _totalSize = totalSize;

            _currentFragmentsCount = 0;
            _expirationDate = DateTime.UtcNow.Add(timeout);

            _fragmentsCount = fragmentsCount;
            _fragments = new byte[fragmentsCount][];

            IsCompleted = false;
        }

        public Message Defragment()
        {
            if(!IsCompleted)
            {
                throw new Exception($"Incomplete, collected {_currentFragmentsCount} out of {_fragmentsCount} fragments");
            }

            var payloadSize = _totalSize - FragmentFormatter.ProtocolMinMessageSize;
            var buffer = new byte[payloadSize];

            var index = 0;
            foreach (var msg in _fragments)
            {
                Array.Copy(msg, 0, buffer, index, msg.Length);
                index += msg.Length;
            }

            var result = new Message(_messageType, _messageId, buffer);

            return result;
        }

        public void AddPayloadFragment(int id, byte[] payload)
        {
            _fragments[id] = payload;
            _currentFragmentsCount++;
            IsCompleted = _currentFragmentsCount == _fragmentsCount;
        }
    }
}
