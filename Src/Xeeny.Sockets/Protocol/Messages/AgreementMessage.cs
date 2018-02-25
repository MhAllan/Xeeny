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

        public static AgreementMessage ReadMessage(byte[] buffer)
        {
            var fragmentSize = BitConverter.ToInt32(buffer, _fragmentSizeIndex);
            var timeout = BitConverter.ToInt32(buffer, _timeoutIndex);

            var result = new AgreementMessage(fragmentSize, timeout);

            return result;
        }

        public static void Write(AgreementMessage message, byte[] buffer)
        {
            var fragmentSize = BitConverter.GetBytes(message.FragmentSize);
            var timeout = BitConverter.GetBytes(message.Timeout);

            buffer[_messageTypeIndex] = (byte)MessageType.Agreement;
            BufferHelper.CopyToIndex(fragmentSize, buffer, _fragmentSizeIndex);
            BufferHelper.CopyToIndex(timeout, buffer, _timeoutIndex);
        }
    }
}
