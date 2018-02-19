using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets.Protocol.Messages
{
    readonly struct CloseMessage
    {
        readonly static MessageType _messageType = MessageType.Close;

        public readonly static CloseMessage Instance = new CloseMessage();
        public readonly static ArraySegment<byte> Bytes = new ArraySegment<byte>(new byte[] { (byte)_messageType });

        public MessageType MessageType => _messageType;

        public static void WriteMessage(byte[] buffer, int offset, int count)
        {
            buffer[offset] = (byte)_messageType;
        }
    }
}
