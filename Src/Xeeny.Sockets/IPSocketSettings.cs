using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets
{
    public class IPSocketSettings : SocketSettings
    {
        public IPVersion IPVersion { get; set; }

        public IPSocketSettings()
        {
            IPVersion = IPVersion.IPv6;
        }
    }
}
