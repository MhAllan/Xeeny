using Microsoft.Extensions.Logging;
using Xeeny.Sockets.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Sockets.RawSockets
{
    public abstract class RawSocket : SocketBase
    {
        readonly IPSocketSettings _settings;

        Socket _socket;
        IPAddress _remoteIP;

        public RawSocket(Socket socket, IPSocketSettings settings, ILogger logger)
            : base(settings, logger)
        {
            _socket = socket;
            _settings = settings;

            InitializeSocket();
        }

        public RawSocket(Uri uri, IPSocketSettings settings, ILogger logger)
            : base(uri, settings, logger)
        {
            _settings = settings;
        }

        protected abstract Socket CreateNewSocket(IPAddress ip);

        void InitializeSocket()
        {
            _socket.SendTimeout = _settings.Timeout.TotalMilliseconds;

            _socket.ReceiveTimeout = _settings.ReceiveTimeout.TotalMilliseconds;

            _socket.SendBufferSize = _settings.SendBufferSize;
            _socket.ReceiveBufferSize = _settings.ReceiveBufferSize;

            _socket.NoDelay = true;

            if (_socket.Connected)
            {
                State = ConnectionState.Connected;
            }
        }

        async Task<IPAddress> GetIP(Uri uri, IPVersion ipVersion)
        {
            if (_remoteIP == null)
            {
                var host = uri.DnsSafeHost;

                if (IPAddress.TryParse(host, out IPAddress address))
                {
                    _remoteIP = address;
                }

                else
                {
                    var hostEntry = await Dns.GetHostEntryAsync(host);
                    if (hostEntry == null)
                    {
                        throw new Exception($"Couldn't resolve host ip {host}");
                    }
                    var addressList = hostEntry.AddressList;
                    IEnumerable<IPAddress> addresses;
                    if (ipVersion == IPVersion.IPv6)
                    {
                        addresses = addressList.Where(x => x.AddressFamily == AddressFamily.InterNetworkV6);
                        if (!addresses.Any())
                        {
                            addresses = addressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork);
                            if (!addresses.Any())
                            {
                                throw new Exception($"Couldn't find IP for host {host}");
                            }
                        }
                    }
                    else if (ipVersion == IPVersion.IPv4)
                    {
                        addresses = addressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork);
                        if (!addresses.Any())
                        {
                            throw new Exception($"Couldn't find IPv4 for host {host}");
                        }
                    }
                    else
                    {
                        throw new NotSupportedException(ipVersion.ToString());
                    }

                    _remoteIP = addresses.First();
                }
            }
            return _remoteIP;
        }

        protected override async Task OnConnect(Uri uri, CancellationToken ct)
        {
            if(_socket == null)
            {
                var ip = await GetIP(uri, _settings.IPVersion);
                _socket = CreateNewSocket(ip);
                InitializeSocket();
            }
            await _socket.ConnectAsync(_remoteIP, uri.Port);
        }
        protected override void OnClose(CancellationToken ct)
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            try
            {
                _socket.Close();
            }
            catch { }
            finally
            {
                try
                {
                    _socket.Dispose();
                }
                catch { }
            }
        }

        protected override async Task<byte[]> Receive(ArraySegment<byte> receiveBuffer, MessageParser parser, 
            CancellationToken ct)
        {
            using (var ms = new MemoryStream())
            {
                bool accepted = false;
                int msgSize = 0;
                int received = 0;
                do
                {
                    var size = await _socket.ReceiveAsync(receiveBuffer, SocketFlags.None)
                                            .ConfigureAwait(false);
                    if (!accepted)
                    {
                        parser.ValidateSize(receiveBuffer, out msgSize);
                        accepted = true;
                    }
                    await ms.WriteAsync(receiveBuffer.Array, 0, size);

                    received += size;
                }
                while (received < msgSize);

                return ms.ToArray();
            }
        }

        protected override async Task Send(IEnumerable<ArraySegment<byte>> segments, CancellationToken ct)
        {
            foreach (var segment in segments)
            {
                await _socket.SendAsync(segment, SocketFlags.None)
                             .ConfigureAwait(false);
            }
        }
    }
}
