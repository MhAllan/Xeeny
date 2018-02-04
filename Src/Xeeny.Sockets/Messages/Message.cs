using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets.Messages
{
    public struct Message
    {
        public MessageType MessageType { get; set; }

        public Guid Id { get; set; }

        public bool IsEncrypted { get; set; }

        public byte[] Address { get; set; }

        public byte[] Payload { get; set; }
    }
}
