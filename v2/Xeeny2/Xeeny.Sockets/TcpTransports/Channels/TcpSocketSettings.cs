using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Xeeny.Transports;

namespace Xeeny.Sockets.TcpTransports.Channels
{
    public class TcpSocketSettings
    {
        public Uri Uri { get; }
        public IPAddress IP { get; }
        public int Port { get; }
        public SocketFlags SocketFlags { get; set; } = SocketFlags.None;
        public AddressFamily AddressFamily { get; private set; }

        IPVersion _IPVersion;
        public IPVersion IPVersion
        {
            get => _IPVersion;
            set
            {
                _IPVersion = value;
                if (_IPVersion == IPVersion.IPv4)
                    AddressFamily = AddressFamily.InterNetwork;
                else if (_IPVersion == IPVersion.IPv6)
                    AddressFamily = AddressFamily.InterNetworkV6;
                else
                    throw new NotSupportedException(_IPVersion.ToString());
            }
        }

        public TcpSocketSettings(Uri uri)
        {
            Uri = uri;
            IPVersion = IPVersion.IPv4;
            IP = SocketTools.GetIP(uri, IPVersion);
            Port = uri.Port;
        }
    }
}
