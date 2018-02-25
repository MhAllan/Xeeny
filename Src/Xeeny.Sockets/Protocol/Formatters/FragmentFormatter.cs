using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xeeny.Sockets.Protocol.Messages;
using Xeeny.Sockets.Protocol.Results;

namespace Xeeny.Sockets.Protocol.Formatters
{
    class FragmentFormatter : IFormatter
    {
        const byte _messageTypeIndex = 0; //1 byte enum
        const byte _idIndex = 1; //16 bytes guid
        const byte _totalSizeIndex = 17; //4 bytes int
        const byte _fragmentIdIndex = 21; //4 bytes int
        const byte _payloadIndex = 25;

        public const byte ProtocolMinMessageSize = _payloadIndex;

        public int MinMessageSize => ProtocolMinMessageSize;
        public int MaxMessageSize => _maxMessageSize;

        readonly int _maxMessageSize;
        readonly int _remoteFragmentSize;
        readonly int _remoteFragmentPayloadSize;
        readonly TimeSpan _waitFragmentsTimeout;
        readonly Timer _timer;

        ConcurrentDictionary<Guid, FragmentCollection> _fragmentCollections =
            new ConcurrentDictionary<Guid, FragmentCollection>();

        public FragmentFormatter(int maxMessageSize, int remoteFragmentSize, TimeSpan waitFragmentsTimeout)
        {
            if (maxMessageSize < MinMessageSize)
            {
                throw new Exception($"Minimum message size is {MinMessageSize}");
            }
            if (maxMessageSize < remoteFragmentSize)
            {
                throw new Exception($"{nameof(remoteFragmentSize)} can not be larger than {nameof(maxMessageSize)}");
            }
            if (remoteFragmentSize < MinMessageSize)
            {
                throw new Exception($"{nameof(remoteFragmentSize)} can not be less than {MinMessageSize}");
            }

            _maxMessageSize = maxMessageSize;
            _remoteFragmentSize = remoteFragmentSize;
            _remoteFragmentPayloadSize = _remoteFragmentSize - MinMessageSize;
            _waitFragmentsTimeout = waitFragmentsTimeout;
            _timer = new Timer(t => Clean(), null, 0, 500);
        }

        bool _isCleaning;
        void Clean()
        {
            if (_isCleaning)
                return;
            lock (this)
            {
                if (_isCleaning)
                    return;

                _isCleaning = true;
                try
                {
                    foreach (var item in _fragmentCollections.Values)
                    {
                        if (item.IsExpired)
                        {
                            _fragmentCollections.TryRemove(item.MessageId, out FragmentCollection _);
                        }
                    }
                }
                finally
                {
                    _isCleaning = false;
                }
            }
        }

        public ReadResult ReadMessage(byte[] buffer, int count)
        {
            ValidateBufferLength(count);

            var fragment = ReadFragment(buffer, count);
            var message = fragment.PartialMessage;
            var totalSize = fragment.TotalSize;

            if (fragment.TotalSize <= _remoteFragmentSize)
            {
                return new ReadResult(message, true);
            }

            var messageType = message.MessageType;
            var messageId = message.Id;
            var payload = message.Payload;
            var payloadSize = totalSize - MinMessageSize;

            var fragmentId = fragment.FragmentId;

            if (!_fragmentCollections.TryGetValue(messageId, out FragmentCollection fc))
            {
                var fragmentsCount = (int)Math.Max(1, Math.Ceiling((double)payloadSize / _remoteFragmentPayloadSize));

                fc = new FragmentCollection(messageType, messageId, totalSize, fragmentsCount, _waitFragmentsTimeout);

                _fragmentCollections.TryAdd(messageId, fc);
            }

            fc.AddPayloadFragment(fragmentId, payload);

            if (fc.IsCompleted)
            {
                _fragmentCollections.TryRemove(messageId, out FragmentCollection _);

                var result = fc.Defragment();

                return new ReadResult(result, true);
            }
            else
            {
                return new ReadResult();
            }
        }

        public WriteResult WriteMessage(Message message, byte[] buffer, int fragmentSize)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            ValidateBufferLength(buffer.Length);

            var items = Fragment();
            return new WriteResult(items);

            IEnumerable<ArraySegment<byte>> Fragment()
            {
                var totalSize = MinMessageSize;
                var payload = message.Payload;
                if(payload != null)
                {
                    totalSize += payload.Length;
                }

                if(totalSize <= fragmentSize)
                {
                    var fragment = new Fragment(message, totalSize, -1);
                    WriteFragment(fragment, buffer);
                    var result = new ArraySegment<byte>(buffer, 0, totalSize);

                    yield return result;
                    yield break;
                }

                var messageType = message.MessageType;
                var messageId = message.Id;
                var payloadSize = payload.Length;

                int index = 0;
                var fragmentId = 0;
                var partialPayloadSize = fragmentSize - _payloadIndex;

                while(true)
                {
                    var take = Math.Min(partialPayloadSize, payloadSize - index);
                    var partialPayload = BufferHelper.GetSubArray(payload, index, take);

                    var partialMessage = new Message(messageType, messageId, partialPayload);
                    var fragmnet = new Fragment(partialMessage, totalSize, fragmentId);
                    var len = WriteFragment(fragmnet, buffer);

                    var partialResult = new ArraySegment<byte>(buffer, 0, len);

                    yield return partialResult;

                    index += take;
                    if (index == payloadSize)
                    {
                        break;
                    }
                    else
                    {
                        fragmentId++;
                    }
                }
            }
        }

        public void Dispose()
        {
            _fragmentCollections = null;
            _timer.Dispose();
        }

        void ValidateBufferLength(int count)
        {
            if (count < MinMessageSize)
            {
                throw new Exception($"Provided read buffer size is {count}, Must be between " +
                    $"[{MinMessageSize} - {MaxMessageSize}]");
            }
            if (count > MaxMessageSize)
            {
                throw new Exception($"Provided read buffer size is {count}, Must be between " +
                    $"[{MinMessageSize} - {MaxMessageSize}]");
            }
        }

        static Fragment ReadFragment(byte[] buffer, int count)
        {
            var messageType = (MessageType)buffer[_messageTypeIndex];
            var id = new Guid(BufferHelper.GetSubArray(buffer, _idIndex, 16));
            var size = BitConverter.ToInt32(buffer, _totalSizeIndex);
            var fragmentId = BitConverter.ToInt32(buffer, _fragmentIdIndex);

            byte[] payload = null;
            var payloadSize = count - _payloadIndex;
            if (payloadSize > 0)
            {
                payload = BufferHelper.GetSubArray(buffer, _payloadIndex, payloadSize);
            }

            var partialMessage = new Message(messageType, id, payload);
            var fragmnet = new Fragment(partialMessage, size, fragmentId);

            return fragmnet;
        }

        static int WriteFragment(Fragment fragment, byte[] buffer)
        {
            int size = _payloadIndex;

            var msg = fragment.PartialMessage;
            buffer[_messageTypeIndex] = (byte)msg.MessageType;
            BufferHelper.CopyToIndex(msg.Id.ToByteArray(), buffer, _idIndex);
            BufferHelper.CopyToIndex(BitConverter.GetBytes(fragment.TotalSize), buffer, _totalSizeIndex);
            BufferHelper.CopyToIndex(BitConverter.GetBytes(fragment.FragmentId), buffer, _fragmentIdIndex);

            var payload = msg.Payload;
            if (payload != null)
            {
                BufferHelper.CopyToIndex(payload, buffer, _payloadIndex);
                size += payload.Length;
            }

            return size;
        }
    }
}
