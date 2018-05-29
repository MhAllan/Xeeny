using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports
{
    public class ArraySegmentWriter
    {
        int _count;
        int _index;
        ArraySegment<byte> _segment;

        public bool CanWrite => LeftSpace > 0;

        public int LeftSpace => _count - _index;

        public ArraySegmentWriter(ArraySegment<byte> segment)
        {
            _segment = segment;
            _count = segment.Count;
        }

        public int Write(byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length == 0)
                return 0;

            var space = _count - _index;

            var writeCount = Math.Min(space, count);

            _segment.Array.WriteArray(_index, data, offset, writeCount);

            _index += writeCount;

            return writeCount;
        }
    }
}
