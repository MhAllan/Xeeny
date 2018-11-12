using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports.Channels;
using Xeeny.Transports.Connections;

namespace Xeeny.Sockets.SslTransports.Channels
{
    class SslClientChannel : PipelineTransportChannel<SslStreamPipelineChannel>
    {
        X509Certificate X509Certificate => NextChannel.X509Certificate;
        string CertificateName => NextChannel.CertificateName;

        SslStream _sslStream;

        public SslClientChannel(SslStreamPipelineChannel nextChannel) : base(nextChannel)
        {
        }

        public override async Task Connect(CancellationToken ct)
        {
            await base.Connect(ct);

            _sslStream = new SslStream(NextChannel.Stream);
            switch(ConnectionSide)
            {
                case ConnectionSide.Client:
                    {
                        await _sslStream.AuthenticateAsClientAsync(CertificateName, null, SslProtocols.Tls12, false);
                        break;
                    }
                case ConnectionSide.Server:
                    {
                        await _sslStream.AuthenticateAsServerAsync(X509Certificate, false, SslProtocols.Tls12, true);
                        break;
                    }
                default: throw new NotSupportedException(ConnectionSide.ToString());
            }
        }

        public override Task Send(ArraySegment<byte> data, CancellationToken ct)
        {
            return _sslStream.WriteAsync(data.Array, data.Offset, data.Count, ct);
        }

        public override Task<int> Receive(ArraySegment<byte> buffer, CancellationToken ct)
        {
            return _sslStream.ReadAsync(buffer.Array, buffer.Offset, buffer.Count, ct);
        }

        public override async Task Close(CancellationToken ct)
        {
            try
            {
                await base.Close(ct);
            }
            finally
            {
                try
                {
                    _sslStream.Dispose();
                }
                catch { }
            }
        }
    }
}
