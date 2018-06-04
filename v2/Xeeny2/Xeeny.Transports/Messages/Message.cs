using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Serialization.Abstractions;

namespace Xeeny.Transports.Messages
{
    public class Message
    {
        public const byte MessageHeader = 1 + 16; //messagetype + id

        public readonly MessageType MessageType;
        public readonly Guid Id;
        public readonly byte[] Payload;

        public Dictionary<ushort, string> Properties { get; set; } = new Dictionary<ushort, string>();

        public Message(MessageType messageType, Guid id, byte[] payload = null)
        {
            MessageType = messageType;
            Id = id;
            Payload = payload;
        }

        public Message(MessageType messageType, byte[] payload = null)
        {
            MessageType = messageType;
            Id = Guid.Empty;
            Payload = payload;
        }

        public Message(ArraySegment<byte> data)
        {

        }

        public Message(byte[] data)
        {

        }

        //public ArraySegment<byte> GetDataAsSegment()
        //{
        //    var data = GetData();
        //    return new ArraySegment<byte>(data);
        //}

        //public byte[] GetData()
        //{
        //    var length = GetDataLength();
        //    var data = new byte[length];
        //    GetData(data, 0);

        //    return data;
        //}

        //int GetDataLength()
        //{
        //    int length = _minMessageSize;
        //    if (Payload != null)
        //    {
        //        length += Payload.Length;
        //    }

        //    foreach (var kv in Properties)
        //    {
        //        length += 4; //key integer
        //        length += Encoding.ASCII.GetByteCount(kv.Value);
        //    }
        //    return length;
        //}
    }
}
