using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Xeeny.Sockets.TcpTransport
{
    public class TcpSocketSettings
    {
        public SocketFlags SocketFlags { get; set; } = SocketFlags.None;
    }
}
