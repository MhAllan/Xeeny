using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports;

namespace Xeeny.Sockets
{
    class SslSocket : ISocket
    {
        public Stream Stream => _sslStream;

        public bool Connected => _socket.Connected;

        readonly ISocket _socket;
        readonly SslStream _sslStream;
        readonly X509Certificate2 _x509Certificate;
        readonly string _certName;

        public SslSocket(ISocket socket, X509Certificate2 x509Certificate, string certName)
        {
            _socket = socket;
            _x509Certificate = x509Certificate;
            _certName = certName;
            _sslStream = new SslStream(socket.Stream);
        }

        public async Task ConnectAsServer(CancellationToken ct)
        {
            await _sslStream.AuthenticateAsServerAsync(_x509Certificate, false, SslProtocols.Tls12, true);
        }

        public async Task ConnectAsClient(IPAddress ipAddress, int port, CancellationToken ct)
        {
            await _socket.ConnectAsClient(ipAddress, port, ct);
            await _sslStream.AuthenticateAsClientAsync(_certName, null, SslProtocols.Tls12, false);
        }

        public Task SendAsync(ArraySegment<byte> segment, SocketFlags flags, CancellationToken ct)
        {
            return _sslStream.WriteAsync(segment.Array, segment.Offset, segment.Count, ct);
        }

        public Task<int> ReceiveAsync(ArraySegment<byte> segment, SocketFlags flags, CancellationToken ct)
        {
            return _sslStream.ReadAsync(segment.Array, segment.Offset, segment.Count, ct);
        }

        public void Close()
        {
            _socket.Close();
            _sslStream.Dispose();
        }
    }
}
