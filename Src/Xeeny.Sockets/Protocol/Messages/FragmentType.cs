using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets.Protocol.Messages
{
    public enum FragmentType : byte
    {
        None,
        Fragment,
        MessageEnd
    }
}
