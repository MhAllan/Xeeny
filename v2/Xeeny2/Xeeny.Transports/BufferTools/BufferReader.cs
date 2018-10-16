using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports
{
    public static class BufferReader
    {
        public static int ReadByte(this byte[] buffer, in int index, out byte result)
        {
            result = buffer[index];
            return index + 1;
        }
        public static int ReadInt16(this byte[] buffer, in int index, out int result)
        {
            result = BitConverter.ToInt16(buffer, index);
            return index + 2;
        }
        public static int ReadInt32(this byte[] buffer, in int index, out int result)
        {
            result = BitConverter.ToInt32(buffer, index);
            return index + 4;
        }
        public static int ReadGuid(this byte[] buffer, in int index, out Guid guid)
        {
            guid = new Guid(buffer.GetSubArray(index, 16));
            return index + 16;
        }
        public static int ReadAsciiString(this byte[] buffer, in int index, in int count, out string result)
        {
            result = Encoding.ASCII.GetString(buffer, index, count);
            return index + count;
        }

        public static int ReadSubArray(this byte[] buffer, in int index, in int count, out byte[] result)
        {
            result = GetSubArray(buffer, index, count);
            return index + count;
        }

        public static byte[] GetSubArray(this byte[] buffer, in int index, in int count)
        {
            var result = new byte[count];
            result.WriteArray(0, buffer, index, count);

            return result;
        }
    }
}
