using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports.MessageFraming
{
    public class ConcurrentStreamChannelSettings
    {
        public int SendBufferSize { get; set; } = 4096;
        public int ReceiveBufferSize { get; set; } = 4096;
        public int MaxMessageSize { get; set; } = 1024 * 1000 * 1000;
        public ConnectionTimeout ReceiveTimeout { get; set; } = ConnectionTimeout.FromSeconds(600);
    }
}
