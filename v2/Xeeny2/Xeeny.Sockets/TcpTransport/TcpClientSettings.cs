using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Xeeny.Sockets.TcpTransport
{
    public class TcpClientSettings : TcpSocketSettings
    {
        public readonly IPAddress IP;
        public readonly int Port;

        public TcpClientSettings(Uri uri, IPVersion iPVersion)
        {
            IP = SocketTools.GetIP(uri, iPVersion);
            Port = uri.Port;
        }
    }
}
