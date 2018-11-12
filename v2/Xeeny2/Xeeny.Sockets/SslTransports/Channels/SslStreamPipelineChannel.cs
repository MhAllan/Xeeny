using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xeeny.Transports.Channels;

namespace Xeeny.Sockets.SslTransports.Channels
{
    abstract class SslStreamPipelineChannel : PipelineTransportChannel
    {
        public abstract Stream Stream { get; }
        
        public string CertificateName { get; }

        public X509Certificate X509Certificate { get; }

        public SslStreamPipelineChannel(TransportChannel nextChannel) : base(nextChannel)
        {
        }
    }
}
