using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets
{
    public enum ConnectionState
    {
        None,
        Connecting,
        Connected,
        Closing,
        Closed
    }
}
