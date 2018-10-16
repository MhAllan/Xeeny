using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports.Connections
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
