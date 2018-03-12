using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports.Buffers
{
    class RichBuffer : IDisposable
    {
        public int CurrentSize => _writeIndex - _offset;
        public byte this[int index]
        {
            get
            {
                if(!TryAdjustIndex(ref index))
                {
                    throw new ArgumentException(nameof(index));
                }
                return _buffer[index];
            }
        }


        readonly int _blockSize;

        byte[] _buffer;
        int _offset;
        int _writeIndex;

        public RichBuffer(int blockSize)
        {
            _blockSize = blockSize;
            _buffer = GetBuffer(1);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            ExpandBuffer(count);

            Buffer.BlockCopy(buffer, offset, _buffer, _writeIndex, count);
            _writeIndex += count;
        }

        public byte[] Read(int offset, int count, bool trim = false)
        {
            if(!TryAdjustIndex(ref offset))
            {
                throw new ArgumentException(nameof(offset));
            }
            if(count <= 0)
            {
                throw new ArgumentException(nameof(count));
            }

            var result = new byte[count];
            Buffer.BlockCopy(_buffer, offset, result, 0, count);

            if(trim)
            {
                PrivateTrim(offset);
            }
            return result;
        }

        public int Read(byte[] buffer, bool trim = false)
        {
            if(buffer == null || buffer.Length == 0)
            {
                throw new ArgumentException(nameof(buffer));
            }
            var count = Math.Min(buffer.Length, CurrentSize);
            if (count > 0)
            {
                Buffer.BlockCopy(_buffer, _offset, buffer, 0, count);
                if (trim)
                {
                    PrivateTrim(_offset);
                }
            }
            return count;
        }

        public bool TryReadInteger(int offset, out int result)
        {
            if (CurrentSize >= 4)
            {
                if (TryAdjustIndex(ref offset))
                {
                    result = BitConverter.ToInt32(_buffer, offset);
                    return true;
                }
            }
            result = 0;
            return false;
        }

        void ExpandBuffer(int count)
        {
            if (count <= 0)
                return;

            var writableSize = _buffer.Length - _writeIndex;
            if (count <= writableSize)
            {
                return;
            }
            else
            {
                var dataSize = _writeIndex - _offset;
                double newSize = dataSize + count;
                var n = (int)Math.Ceiling(newSize / _blockSize);
                var buffer = GetBuffer(n);
                if (dataSize > 0)
                {
                    Buffer.BlockCopy(_buffer, _offset, buffer, 0, dataSize);
                }
                ReturnBuffer(_buffer);
                _buffer = buffer;
                _offset = 0;
                _writeIndex = dataSize;
            }
        }

        public void Trim(int offset)
        {
            if(!TryAdjustIndex(ref offset))
            {
                throw new ArgumentException(nameof(offset));
            }
            PrivateTrim(offset);
        }

        void PrivateTrim(int offset)
        {
            if (offset <= 0)
            {
                return;
            }
            if (offset > _writeIndex)
            {
                offset = _writeIndex;
            }

            var dataSize = _writeIndex - offset;
            var n = dataSize == 0 ? 1 : (int)Math.Ceiling((double)dataSize / _blockSize);
            var dataBufferSize = n * _blockSize;
            if(dataBufferSize < _buffer.Length)
            {
                var buffer = GetBuffer(n);
                Buffer.BlockCopy(_buffer, offset, buffer, 0, dataSize);
                ReturnBuffer(_buffer);
                _buffer = buffer;
                _offset = 0;
                _writeIndex = dataSize;
            }
            else
            {
                _offset = offset;
            }
        }

        bool TryAdjustIndex(ref int offset)
        {
            if(offset < 0 || offset > CurrentSize)
            {
                return false;
            }
            offset += _offset;
            return true;
        }

        byte[] GetBuffer(int blockCount)
        {
            return ArrayPool<byte>.Shared.Rent(_blockSize * blockCount);
        }

        void ReturnBuffer(byte[] buffer)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        public void Dispose()
        {
            ReturnBuffer(_buffer);
            _buffer = null;
        }
    }
}
