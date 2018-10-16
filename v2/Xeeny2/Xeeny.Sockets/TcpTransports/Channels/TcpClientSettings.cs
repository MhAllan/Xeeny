using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Xeeny.Sockets.TcpTransports.Channels
{
    public class TcpClientSettings : TcpSocketSettings
    {
        public TcpClientSettings(Uri uri) : base(uri)
        {
            
        }
    }
}
