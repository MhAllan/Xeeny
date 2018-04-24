using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports;

namespace Xeeny.Http
{
    public class WebSocketTransportSettings : TransportSettings
    {
        /// <summary>
        /// Message Framing Protocol
        /// </summary>
        public FramingProtocol FramingProtocol { get; set; }

        public WebSocketTransportSettings(ConnectionSide connectionSide)
            : base(connectionSide)
        {

        }
    }
}
