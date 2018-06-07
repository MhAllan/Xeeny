using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports.Messages;

namespace Xeeny
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

        //public static int WriteMessage(this byte[] buffer, int offset, Message message)
        //{
        //    var space = buffer.Length - offset;
        //    var length = message.GetBinaryLength();

        //    if (space < length)
        //    {
        //        throw new Exception($"No enough space, message size is {length} while available is {space}");
        //    }

        //    var index = buffer.WriteByte(offset, (byte)message.MessageType);
        //    index = buffer.WriteGuid(index, message.Id);
        //    var payload = message.Payload;
        //    if (payload != null)
        //    {
        //        index = buffer.WriteArray(index, payload);
        //    }
        //    var props = message.Properties;
        //    foreach (var p in props)
        //    {
        //        var k = p.Key;
        //        var kLen = Encoding.ASCII.GetByteCount(k);
        //        index = buffer.WriteInt32(index, kLen);
        //        index = buffer.WriteAsciiString(index, k);

        //        var val = p.Value;
        //        var vLen = Encoding.ASCII.GetByteCount(val);
        //        index = buffer.WriteInt32(index, vLen);
        //        index = buffer.WriteAsciiString(index, val);
        //    }

        //    return index;
        //}
    }
}
