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
        public bool Connected => _socket.Connected;

        readonly Socket _socket;

        SslStream _sslStream;
        readonly X509Certificate2 _x509Certificate;
        readonly string _certName;
        readonly RemoteCertificateValidationCallback _validationCallback;

        public SslSocket(Socket socket, X509Certificate2 x509Certificate, RemoteCertificateValidationCallback validationCallback)
        {
            _socket = socket;
            _x509Certificate = x509Certificate;
            _validationCallback = validationCallback;
        }

        public SslSocket(Socket socket, string certName, RemoteCertificateValidationCallback validationCallback)
        {
            _socket = socket;
            _certName = certName;
            _validationCallback = validationCallback;
        }

        public async Task ConnectAsServer(CancellationToken ct)
        {
            _sslStream = new SslStream(new NetworkStream(_socket), false, _validationCallback);
            await _sslStream.AuthenticateAsServerAsync(_x509Certificate, false, SslProtocols.Tls12, true);
        }

        public async Task ConnectAsClient(IPAddress ipAddress, int port, CancellationToken ct)
        {
            await _socket.ConnectAsync(ipAddress, port);
            _sslStream = new SslStream(new NetworkStream(_socket), false, _validationCallback);
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
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Disconnect(false);
                _socket.Close();
            }
            finally
            {
                _socket.Dispose();
                _sslStream?.Dispose();
            }
        }
    }
}
