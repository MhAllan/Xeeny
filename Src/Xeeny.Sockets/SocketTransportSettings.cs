using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports;

namespace Xeeny.Sockets
{
    public class SocketTransportSettings : TransportSettings
    {
        public SecuritySettings SecuritySettings { get; set; }
    }
}
