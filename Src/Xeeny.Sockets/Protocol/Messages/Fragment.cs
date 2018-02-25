using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets.Protocol.Messages
{
    public readonly struct Fragment
    {
        public readonly Message PartialMessage;
        public readonly int TotalSize;
        public readonly int FragmentId;

        public Fragment(Message partialMessage, int totalSize, int fragmentId)
        {
            PartialMessage = partialMessage;
            TotalSize = totalSize;
            FragmentId = fragmentId;
        }
    }
}
