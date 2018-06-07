using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports.Messages;

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

        //public static Message ReadMessage(this byte[] buffer, int index, int count)
        //{
        //    var length = count - index;
        //    if (length < Message.MessageHeader)
        //        throw new Exception("Too small message");

        //    var readIndex = buffer.ReadByte(0, out var msgType);
        //    readIndex = buffer.ReadSubArray(readIndex, 16, out var msgId);
        //    byte[] payload = null;
        //    if(readIndex < length)
        //    {
        //        readIndex = buffer.ReadInt32(readIndex, out var payloadLength);
        //        readIndex = buffer.ReadSubArray(readIndex, payloadLength, out payload);
        //    }

        //    var message = new Message((MessageType)msgType, new Guid(msgId), payload);

        //    while(readIndex < length)
        //    {
        //        readIndex = buffer.ReadInt32(readIndex, out var kLen);
        //        readIndex = buffer.ReadAsciiString(readIndex, kLen, out var key);
        //        readIndex = buffer.ReadInt32(readIndex, out var vLen);
        //        readIndex = buffer.ReadAsciiString(readIndex, vLen, out var value);

        //        message.Properties.Add(key, value);
        //    }

        //    return message;
        //}
    }
}
