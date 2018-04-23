using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets
{
    public class IPSocketSettings : SocketTransportSettings
    {
        public IPVersion IPVersion { get; set; }
        /// <summary>
        /// Message Framing Protocol
        /// </summary>
        public FramingProtocol FramingProtocol { get; set; }

        public IPSocketSettings()
        {
            IPVersion = IPVersion.IPv6;
        }
    }
}
