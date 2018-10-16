using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xeeny.Transports.Messages
{
    public class Message
    {
        public static readonly Message KeepAlive = new Message(MessageType.KeepAlive);
        public static readonly Message Close = new Message(MessageType.Close);
        public static Message CreateNegotiateMsg() => new Message(MessageType.Negotiate);
        public static Message CreateRequest(byte[] payload, Guid id = default) => new Message(MessageType.Request, id == default ? Guid.NewGuid() : id, payload);
        public static Message CreateResponse(Guid id, byte[] payload = null) => new Message(MessageType.Response, id, payload);
        public static Message CreateError(Guid id, byte[] payload = null) => new Message(MessageType.Error, id, payload);

        public readonly MessageType MessageType;
        public readonly Guid Id;
        public readonly byte[] Payload;
        public readonly Dictionary<string, string> Properties = new Dictionary<string, string>();

        private Message(MessageType msgType)
            : this(msgType, Guid.NewGuid(), null)
        {

        }

        private Message(MessageType msgType, Guid msgId, byte[] payload)
        {
            MessageType = msgType;
            Id = msgId;
            Payload = payload;
        }

        internal Message(byte[] data)
        {
            var index = data.ReadByte(0, out var msgType);
            MessageType = (MessageType)msgType;

            index = data.ReadGuid(index, out Id);

            if (index < data.Length)
            {
                index = data.ReadInt32(index, out var pLen);
                index = data.ReadSubArray(index, pLen, out Payload);

                while (index < data.Length)
                {
                    index = data.ReadInt32(index, out var kLen);
                    index = data.ReadAsciiString(index, kLen, out var k);

                    index = data.ReadInt32(index, out var vLen);
                    index = data.ReadAsciiString(index, vLen, out var v);

                    Properties.Add(k, v);
                }
            }
        }

        public byte[] GetData()
        {
            var size = GetSize();
            var buffer = new byte[size];

            Write(buffer, 0);

            return buffer;
        }

        public int GetSize()
        {
            var pLen = Payload == null ? 4 : Payload.Length + 4;
            var propLen = 0;
            foreach(var p in Properties)
            {
                propLen += 8;
                propLen += Encoding.ASCII.GetByteCount(p.Key);
                propLen += Encoding.ASCII.GetByteCount(p.Value);
            }

            return 17 + pLen + propLen;
        }

        public int Write(byte[] buffer, int offset)
        {
            var index = buffer.WriteByte(offset, (byte)MessageType);
            index = buffer.WriteGuid(index, Id);
            var pLen = Payload == null ? 0 : Payload.Length;
            var pIncluded = pLen > 0;
            if(pIncluded)
            {
                index = buffer.WriteInt32(index, pLen);
                index = buffer.WriteArray(index, Payload);
            }
            if(Properties.Any())
            {
                if(!pIncluded)
                {
                    index = buffer.WriteInt32(index, 0);
                }
                foreach(var p in Properties)
                {
                    var k = Encoding.ASCII.GetBytes(p.Key);
                    var v = Encoding.ASCII.GetBytes(p.Value);

                    index = buffer.WriteInt32(index, k.Length);
                    index = buffer.WriteArray(index, k);

                    index = buffer.WriteInt32(index, v.Length);
                    index = buffer.WriteArray(index, v);
                }
            }

            return index;
        }
    }
}
