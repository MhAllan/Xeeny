using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets.Protocol.Messages
{
    public readonly struct Message
    {
        public readonly MessageType MessageType;
        public readonly Guid Id;
        public readonly byte[] Payload;

        public Message(MessageType messageType, Guid id, byte[] payload)
        {
            MessageType = messageType;
            Id = id;

            Payload = payload;
        }
    }
}
