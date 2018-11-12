using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Sockets.TcpTransports.Channels;
using Xeeny.Transports.Channels;

namespace Xeeny.Sockets.SslTransports.Channels
{
    class SslTcpPiplineChannel : SslStreamPipelineChannel
    {
        public override Stream Stream => _networkStream;

        readonly TcpChannel _tcpChannel;
        NetworkStream _networkStream;

        public SslTcpPiplineChannel(TcpChannel tcpChannel) : base(tcpChannel)
        {
            _tcpChannel = tcpChannel;
        }

        public override async Task Connect(CancellationToken ct)
        {
            await base.Connect(ct);
            _networkStream = new NetworkStream(_tcpChannel.Socket);
        }
    }
}
