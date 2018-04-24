using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports;

namespace Xeeny.Sockets
{
    public class SocketTransportSettings : TransportSettings
    {
        public IPVersion IPVersion { get; set; }
        /// <summary>
        /// Message Framing Protocol
        /// </summary>
        public FramingProtocol FramingProtocol { get; set; }

        public SecuritySettings SecuritySettings { get; set; }

        public SocketTransportSettings(ConnectionSide connectionSide)
            : base(connectionSide)
        {
            IPVersion = IPVersion.IPv6;
        }
    }
}
