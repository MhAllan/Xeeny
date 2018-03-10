using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports;

namespace Xeeny.Sockets
{
    public class IPSocketSettings : TransportSettings
    {
        public IPVersion IPVersion { get; set; }

        public IPSocketSettings()
        {
            IPVersion = IPVersion.IPv6;
        }
    }
}
