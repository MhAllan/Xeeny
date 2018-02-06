using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Xeeny.Sockets.Messages;

namespace Xeeny.Sockets.UdpSockets
{
    /// <summary>
    /// This class is not thread safe
    /// </summary>
    class UdpMessageAssembler : IDisposable
    {
        public readonly Guid MessageId;

        public byte[] Message
        {
            get
            {
                if (_isDisposed)
                    throw new Exception($"{nameof(UdpMessageAssembler)} is already disposed");

                if (!IsCompleted)
                    return null;

                if (_result == null)
                {
                    _result = new byte[_totalSize];
                    var offset = 0;
                    foreach (var fragment in _fragments)
                    {
                        Array.Copy(fragment, 0, _result, offset, fragment.Length);
                        offset += fragment.Length;
                    }
                }

                return _result;
            }
        }

        public bool IsCompleted { get; private set; }

        readonly int _totalSize;
        readonly int _maxFragmentSize;

        byte[][] _fragments;
        byte[] _result;
        int _currentSize;
        bool _isDisposed;

        public UdpMessageAssembler(Guid msgId, int totalSize, int maxFragmentSize)
        {
            if (totalSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalSize));
            if (maxFragmentSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxFragmentSize));
            if (totalSize < maxFragmentSize)
                throw new Exception($"{nameof(totalSize)} can not be less than {nameof(maxFragmentSize)}");

            this.MessageId = msgId;

            _totalSize = totalSize;
            _maxFragmentSize = maxFragmentSize;

            var fragmentsCount = (int)Math.Ceiling((double)totalSize / maxFragmentSize);
            _fragments = new byte[fragmentsCount][];
        }

        public bool AddFragment(int index, byte[] fragment)
        {
            if (fragment == null)
                throw new ArgumentNullException(nameof(fragment));

            if (fragment.Length > _maxFragmentSize)
                throw new Exception($"Fragment size can not be more that {_maxFragmentSize}");

            if (_fragments[index] == null)
            {
                _fragments[index] = fragment;
                _currentSize += fragment.Length;

                IsCompleted = _currentSize == _totalSize;
            }

            return IsCompleted;
        }

        public void Dispose()
        {
            _fragments = null;
            _isDisposed = true;
        }
    }
}
