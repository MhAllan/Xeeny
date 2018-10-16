using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Sockets.TcpTransports.Channels;
using Xeeny.Transports;

namespace Xeeny.Sockets.TcpTransports.Client
{
    public class TcpClientTransportSettings : MessageTransportSettings
    {
        public TcpClientSettings SocketSettings { get; }

        internal TcpClientTransportSettings(TcpClientSettings socketSettings)
        {
            SocketSettings = socketSettings;
        }
    }
}
