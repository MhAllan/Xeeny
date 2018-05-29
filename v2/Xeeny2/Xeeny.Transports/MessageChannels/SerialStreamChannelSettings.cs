using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports.MessageChannels
{
    public class SerialStreamChannelSettings
    {
        public int SendBufferSize { get; set; }
        public int ReceiveBufferSize { get; set; }
        public int MaxMessageSize { get; set; }
    }
}
