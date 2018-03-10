using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports
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
