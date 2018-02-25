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
    //[messageType](byte) [total-size](int) [id](guid) [fragment-id](int) [payload]
    class Formatter : IFormatter
    {
        public int MinMessageSize => FragmentReaderWriter.MinMessageSize;
        public int MaxMessageSize => _maxMessageSize;

        readonly int _maxMessageSize;
        readonly int _remoteFragmentSize;
        readonly TimeSpan _waitFragmentsTimeout;
        readonly Timer _timer;

        ConcurrentDictionary<Guid, FragmentCollection> _fragmentCollections =
            new ConcurrentDictionary<Guid, FragmentCollection>();

        public Formatter(int maxMessageSize, int remoteFragmentSize, TimeSpan waitFragmentsTimeout)
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

        public ReadResult ReadMessage(ArraySegment<byte> segment)
        {
            var buffer = segment.Array;
            var offset = segment.Offset;
            var count = segment.Count;
            ValidateBufferLength(count - offset);

            var fragment = FragmentReaderWriter.ReadFragment(buffer, count);
            var message = fragment.PartialMessage;
            var totalSize = fragment.TotalSize;

            if (fragment.TotalSize <= _remoteFragmentSize)
            {
                return new ReadResult(message, true);
            }

            var messageType = message.MessageType;
            var messageId = message.Id;
            var payload = message.Payload;

            var fragmentId = fragment.FragmentId;

            if (!_fragmentCollections.TryGetValue(messageId, out FragmentCollection fc))
            {
                fc = new FragmentCollection(messageType, messageId, totalSize, _waitFragmentsTimeout);

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
                var totalSize = Message.GetSize(message);
                if(totalSize <= fragmentSize)
                {
                    Message.WriteMessage(message, buffer);

                    var result = new ArraySegment<byte>(buffer, 0, totalSize);

                    yield return result;
                    yield break;
                }

                var messageType = message.MessageType;
                var messageId = message.Id;

                var payload = message.Payload;
                var payloadSize = payload.Length;

                var header = new byte[Message.HeaderSize];
                Message.WriteHeader(message, header);

                int index = Message.HeaderSize;
                var fragmentId = 0;
                var fragmentType = FragmentType.Fragment;
                var partialPayloadSize = Message.GetPayloadSize(fragmentSize);

                while(true)
                {
                    var take = Math.Min(partialPayloadSize, totalSize - index);
                    var partialPayload = ArrayHelper.GetSubArray(buffer, index, take);

                    ArrayHelper.Copy(header, buffer);
                    Message.WriteBody(fragmentType, fragmentId, partialPayload, buffer);

                    var partialResult = new ArraySegment<byte>(buffer, 0, fragmentSize);

                    yield return partialResult;

                    index += take;
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
    }
}
