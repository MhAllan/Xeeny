using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports.Buffers
{
    class RichBuffer : IDisposable
    {
        public byte this[int index] => _buffer[_offset + index];
        public int CurrentSize => _writeIndex - _offset;
        
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

        public byte[] Read(int offset, int count)
        {
            offset += _offset;
            var result = new byte[count];
            Buffer.BlockCopy(_buffer, offset, result, 0, count);

            return result;
        }

        public int ReadInteger(int offset)
        {
            if (_writeIndex - _offset >= 4)
            {
                offset += _offset;
                return BitConverter.ToInt32(_buffer, offset);
            }
            return 0;
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
            offset += _offset;
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
