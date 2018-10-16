using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports
{
    static class BufferWriter
    {
        public static int WriteByte(this byte[] destination, in int index, in byte number)
        {
            destination[index] = number;
            return index + 1;
        }
        public static int WriteInt16(this byte[] destination, in int index, in short number)
        {
            return WriteArray(destination, index, BitConverter.GetBytes(number));
        }
        public static int WriteInt32(this byte[] destination, in int index, in int number)
        {
            return WriteArray(destination, index, BitConverter.GetBytes(number));
        }
        public static int WriteGuid(this byte[] destination, in int index, in Guid guid)
        {
            return WriteArray(destination, index, guid.ToByteArray());
        }
        public static int WriteAsciiString(this byte[] destination, in int index, in string ascii)
        {
            return WriteArray(destination, index, Encoding.ASCII.GetBytes(ascii));
        }
        public static int WriteSegment(this byte[] destination, in int index, ArraySegment<byte> segment)
        {
            return WriteArray(destination, index, segment.Array, segment.Offset, segment.Count);
        }
        public static int WriteArray(this byte[] destination, in int index, in byte[] source)
        {
            return WriteArray(destination, index, source, source.Length);
        }
        public static int WriteArray(this byte[] destination, in int index, in byte[] source, in int count)
        {
            return WriteArray(destination, index, source, 0, count);
        }
        public static int WriteArray(this byte[] destination, in int index, in byte[] source, int sourceFrom, in int count)
        {
            Array.Copy(source, sourceFrom, destination, index, count);
            return index + count;
        }
    }
}
