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
using Xeeny.Transports.Channels;

namespace Xeeny.Sockets
{
    class SslSocketChannel : ITransportChannel
    {
        public ConnectionSide ConnectionSide => _connectionSide;
        public string ConnectionName => _connectionName;

        readonly Socket _socket;
        readonly X509Certificate2 _x509Certificate;
        readonly string _certName;
        readonly RemoteCertificateValidationCallback _validationCallback;
        readonly IPAddress _ipAddress;
        readonly int _port;

        readonly ConnectionSide _connectionSide;
        readonly string _connectionName;

        SslStream _sslStream;
        public SslSocketChannel(Socket socket, X509Certificate2 x509Certificate, RemoteCertificateValidationCallback validationCallback, string connectionName)
        {
            _socket = socket;
            _x509Certificate = x509Certificate;
            _validationCallback = validationCallback;
            _connectionName = connectionName;
            _connectionSide = ConnectionSide.Server;
        }

        public SslSocketChannel(Socket socket, IPAddress ipAddress, int port, string certName, RemoteCertificateValidationCallback validationCallback, string connectionName)
        {
            _socket = socket;
            _ipAddress = ipAddress;
            _port = port;
            _certName = certName;
            _validationCallback = validationCallback;
            _connectionName = connectionName;
            _connectionSide = ConnectionSide.Client;
        }

        public Task Connect(CancellationToken ct)
        {
            switch(_connectionSide)
            {
                case ConnectionSide.Server: return ConnectAsServer(ct);
                case ConnectionSide.Client: return ConnectAsClient(ct);
                default: throw new NotSupportedException(_connectionSide.ToString());
            }
        }

        async Task ConnectAsServer(CancellationToken ct)
        {
            _sslStream = new SslStream(new NetworkStream(_socket), false, _validationCallback);
            await _sslStream.AuthenticateAsServerAsync(_x509Certificate, false, SslProtocols.Tls12, true);
        }

        public async Task ConnectAsClient(CancellationToken ct)
        {
            await _socket.ConnectAsync(_ipAddress, _port);
            _sslStream = new SslStream(new NetworkStream(_socket), false, _validationCallback);
            await _sslStream.AuthenticateAsClientAsync(_certName, null, SslProtocols.Tls12, false);
        }

        public Task SendAsync(ArraySegment<byte> segment, CancellationToken ct)
        {
            return _sslStream.WriteAsync(segment.Array, segment.Offset, segment.Count, ct);
        }

        public Task<int> ReceiveAsync(ArraySegment<byte> segment, CancellationToken ct)
        {
            return _sslStream.ReadAsync(segment.Array, segment.Offset, segment.Count, ct);
        }

        public void Close(CancellationToken ct)
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
