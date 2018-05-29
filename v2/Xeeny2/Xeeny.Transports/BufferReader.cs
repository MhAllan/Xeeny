using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports
{
    public static class BufferReader
    {
        public static int ReadInt32(this byte[] buffer, int index, out int result)
        {
            result = BitConverter.ToInt32(buffer, index);
            return index + 4;
        }

        public static int ReadAsciiString(this byte[] buffer, int index, int count, out string result)
        {
            result = Encoding.ASCII.GetString(buffer, index, count);
            return index + count;
        }
    }
}
