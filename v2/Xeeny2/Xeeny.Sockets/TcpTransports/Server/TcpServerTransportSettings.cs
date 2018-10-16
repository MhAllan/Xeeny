using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Sockets.TcpTransports.Channels;
using Xeeny.Transports;

namespace Xeeny.Sockets.TcpTransports.Server
{
    public class TcpServerTransportSettings : MessageTransportSettings
    {
        public TcpSocketSettings SocketSettings { get; }

        internal TcpServerTransportSettings(TcpSocketSettings socketSettings)
        {
            SocketSettings = socketSettings;
        }
    }
}
