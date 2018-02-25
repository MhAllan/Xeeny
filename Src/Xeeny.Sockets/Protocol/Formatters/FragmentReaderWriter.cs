using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Sockets.Protocol.Messages;

namespace Xeeny.Sockets.Protocol.Formatters
{
    public class FragmentReaderWriter
    {
        const byte _messageTypeIndex = 0; //1 byte enum
        const byte _idIndex = 1; //16 bytes guid
        const byte _totalSizeIndex = 17; //4 bytes int
        const byte _fragmentIdIndex = 21; //4 bytes int
        const byte _payloadIndex = 25;

        public static int MinMessageSize => _payloadIndex;


        public static Fragment ReadFragment(byte[] buffer, int count)
        {
            var messageType = (MessageType)buffer[_messageTypeIndex];
            var id = new Guid(ArrayHelper.GetSubArray(buffer, _idIndex, 16));
            var size = BitConverter.ToInt32(buffer, _totalSizeIndex);
            var fragmentId = BitConverter.ToInt32(buffer, _fragmentIdIndex);

            byte[] payload = null;
            var payloadSize = count - _payloadIndex;
            if(payloadSize > 0)
            {
                payload = ArrayHelper.GetSubArray(buffer, _payloadIndex, payloadSize);
            }

            var partialMessage = new Message(messageType, id, payload);
            var fragmnet = new Fragment(partialMessage, size, fragmentId);

            return fragmnet;
        }

        public static void WriteFragment(Fragment fragment, byte[] buffer)
        {
            var msg = fragment.PartialMessage;
            buffer[_messageTypeIndex] = (byte)msg.MessageType;
            ArrayHelper.CopyToIndex(msg.Id.ToByteArray(), buffer, _idIndex);
            ArrayHelper.CopyToIndex(BitConverter.GetBytes(fragment.TotalSize), buffer, _totalSizeIndex);
            ArrayHelper.CopyToIndex(BitConverter.GetBytes(fragment.FragmentId), buffer, _fragmentIdIndex);
            if(msg.Payload != null)
            {
                ArrayHelper.CopyToIndex(msg.Payload, buffer, _payloadIndex);
            }
        }
    }
}
