using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xeeny.Sockets.Messages;
using Xeeny.Sockets.RawSockets;

namespace Xeeny.Sockets.TcpSockets
{
    public class TcpSocket : RawSocket
    {
        public TcpSocket(Socket socket, IPSocketSettings settings, ILoggerFactory loggerFactory) 
            : base(socket, settings, loggerFactory.CreateLogger(nameof(TcpSocket)))
        {
        }

        public TcpSocket(Uri uri, IPSocketSettings settings, ILoggerFactory loggerFactory) 
            : base(uri, settings, loggerFactory.CreateLogger(nameof(TcpSocket)))
        {

        }

        protected override Socket CreateNewSocket(IPAddress ip)
        {
            var family = ip.AddressFamily;
            var client = new TcpClient(family);
            var socket = client.Client;

            if (family == AddressFamily.InterNetworkV6)
            {
                socket.DualMode = true;
            }

            return socket;
        }

    }
}
