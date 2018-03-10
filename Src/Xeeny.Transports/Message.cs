using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports
{
    public readonly struct Message
    {
        public static readonly Message KeepAliveMessage = new Message(MessageType.KeepAlive, Guid.Empty, null);
        public static readonly Message CloseMessage = new Message(MessageType.Close, Guid.Empty, null);

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
