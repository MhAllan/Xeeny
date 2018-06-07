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

        public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();

        private Message(MessageType messageType, Guid id, byte[] payload = null)
        {
            MessageType = messageType;
            Id = id;
            Payload = payload;
        }

        public static Message CreateRequest(byte[] payload = null)
        {
            return new Message(MessageType.Request, Guid.NewGuid(), payload);
        }

        public static Message CreateResponse(Guid id, byte[] payload = null)
        {
            return new Message(MessageType.Response, id, payload);
        }

        public static Message CreateError(Guid id, byte[] payload = null)
        {
            return new Message(MessageType.Error, id, payload);
        }
    }
}
