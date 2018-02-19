using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets.Protocol.Messages
{
    public readonly struct Message
    {
        public readonly MessageType MessageType;
        public readonly Guid Id;

        public readonly FragmentType FragmentType;
        public readonly int FragmentId;

        public readonly byte[] Payload;

        public Message(MessageType messageType, Guid id, byte[] payload)
        {
            MessageType = messageType;
            Id = id;

            FragmentType = FragmentType.None;
            FragmentId = -1;

            Payload = payload;
        }

        public Message(MessageType messageType, Guid id, FragmentType fragmentType, int fragmentId, byte[] payload)
        {
            MessageType = messageType;
            Id = id;

            FragmentType = fragmentType;
            FragmentId = fragmentId;

            Payload = payload;
        }


        const byte _messageTypeIndex = 0; //1 byte
        const byte _idIndex = 1; //16 bytes guid
        const byte _fragmentTypeIndex = _idIndex + 16; //1 byte
        const byte _fragmentIdIndex = _fragmentTypeIndex + 1; //4 bytes
        const byte _payloadIndex = _fragmentIdIndex + 4;

        public const byte HeaderSize = _fragmentTypeIndex;
        public const byte MinMessageSize = _payloadIndex;

        public static int GetSize(Message message)
        {
            var pl = message.Payload;
            return MinMessageSize + (pl == null ? 0 : pl.Length);
        }

        public static int GetPayloadSize(int messageSize)
        {
            return messageSize - _payloadIndex;
        }

        public static Message ReadFragment(byte[] buffer, int offset, int count)
        {
            var messageType = (MessageType)buffer[offset + _messageTypeIndex];
            var id = new Guid(new Span<byte>(buffer,_idIndex, 16).ToArray());

            var fragmentType = (FragmentType)buffer[_fragmentTypeIndex];
            var fragmentId = BitConverter.ToInt32(buffer, _fragmentIdIndex);

            var payloadSize = count - _payloadIndex;
            var payload = new Span<byte>(buffer, _payloadIndex, payloadSize).ToArray();

            var result = new Message(messageType, id, fragmentType, fragmentId, payload);

            return result;
        }

        public static void WriteMessage(Message message, byte[] buffer)
        {
            WriteHeader(message, buffer);
            WriteBody(message.FragmentType, message.FragmentId, message.Payload, buffer);
        }

        public static void WriteHeader(Message message, byte[] buffer)
        {
            buffer[_messageTypeIndex] = (byte)message.MessageType;
            var id = message.Id.ToByteArray();

            ArrayHelper.CopyToIndex(id, buffer, _idIndex);
        }

        public static void WriteBody(FragmentType fragmentType, int fragmentId, byte[] payload, byte[] buffer)
        {
            buffer[_fragmentTypeIndex] = (byte)fragmentType;
            var fragment = BitConverter.GetBytes(fragmentId);
            ArrayHelper.CopyToIndex(fragment, buffer, _fragmentIdIndex);

            if (payload != null && payload.Length > 0)
            {
                ArrayHelper.CopyToIndex(payload, buffer, _payloadIndex);
            }
        }
    }
}
