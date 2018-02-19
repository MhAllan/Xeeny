using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Sockets.Protocol.Results;

namespace Xeeny.Sockets.Protocol.Messages
{
    readonly struct AgreementMessage
    {
        public MessageType MessageType => MessageType.Agreement;

        public readonly int FragmentSize;
        public readonly int Timeout;

        public AgreementMessage(int fragmentSize, int timeoutMS)
        {
            FragmentSize = fragmentSize;
            Timeout = timeoutMS;
        }


        public static byte MessageFixedSize => _messageFixedSize;

        const byte _messageTypeIndex = 0; //1 byte
        const byte _fragmentSizeIndex = _messageTypeIndex + 1; //4 bytes
        const byte _timeoutIndex = _fragmentSizeIndex + 4; //4 bytes
        const byte _messageFixedSize = _timeoutIndex + 4;

        public static AgreementMessage ReadMessage(ArraySegment<byte> segment)
        {
            return ReadMessage(segment.Array, segment.Offset, segment.Count);
        }

        public static AgreementMessage ReadMessage(byte[] buffer, int offset, int count)
        {
            var fragmentSize = BitConverter.ToInt32(buffer, offset + _fragmentSizeIndex);
            var timeout = BitConverter.ToInt32(buffer, offset + _timeoutIndex);

            var result = new AgreementMessage(fragmentSize, timeout);

            return result;
        }

        public static void Write(AgreementMessage message, byte[] buffer, int offset)
        {
            var fragmentSize = BitConverter.GetBytes(message.FragmentSize);
            var timeout = BitConverter.GetBytes(message.Timeout);

            buffer[offset] = (byte)MessageType.Agreement;
            ArrayHelper.CopyToIndex(fragmentSize, buffer, offset + _fragmentSizeIndex);
            ArrayHelper.CopyToIndex(timeout, buffer, offset + _timeoutIndex);
        }
    }
}
