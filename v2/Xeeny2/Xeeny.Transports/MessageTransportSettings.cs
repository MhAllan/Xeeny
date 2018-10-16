using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports
{
    public class MessageTransportSettings : TransportSettings
    {
        public int SendBufferSize { get; set; } = 4096;
        public int ReceiveBufferSize { get; set; } = 4096;
        public int MaxMessageSize { get; set; } = 1024 * 1000 * 1000;
    }
}
