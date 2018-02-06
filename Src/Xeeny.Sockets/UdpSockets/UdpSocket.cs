using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Sockets.Messages;

namespace Xeeny.Sockets.UdpSockets
{
    //class UdpSocket : SocketBase
    //{
    //    readonly Socket _socket;
    //    readonly IPSocketSettings _settings;

    //    public UdpSocket(Socket socket, IPSocketSettings settings, ILoggerFactory loggerFactory) 
    //        : base(settings, loggerFactory.CreateLogger(nameof(UdpSocket)))
    //    {
    //        _socket = socket;
    //        _settings = settings;
    //    }

    //    public UdpSocket(Uri uri, IPSocketSettings settings, ILoggerFactory loggerFactory)
    //        : base(settings, loggerFactory.CreateLogger(nameof(UdpSocket)))
    //    {
    //        _settings = settings;
    //    }

    //    protected override void OnClose(CancellationToken ct)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    protected override async Task OnConnect(Uri uri, CancellationToken ct)
    //    {
            
    //    }

    //    protected override Task<byte[]> Receive(ArraySegment<byte> receiveBuffer, MessageParser parser, CancellationToken ct)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    protected override Task Send(IEnumerable<ArraySegment<byte>> segments, CancellationToken ct)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
