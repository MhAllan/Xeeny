using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Xeeny.Sockets
{
    static class SocketTools
    {
        static readonly ConcurrentDictionary<string, IPAddress> _resolvedIPs =
            new ConcurrentDictionary<string, IPAddress>();

        public static IPAddress GetIP(Uri uri, IPVersion ipVersion)
        {
            var host = uri.DnsSafeHost;

            if (_resolvedIPs.TryGetValue(host, out var ip))
            {
                return ip;
            }

            if (IPAddress.TryParse(host, out IPAddress address))
            {
                _resolvedIPs.AddOrUpdate(host, address, (k, v) => address);
                return address;
            }

            var hostEntry = Dns.GetHostEntry(host);

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

            var foundAddress = addresses.First();
            _resolvedIPs.AddOrUpdate(host, foundAddress, (k, v) => foundAddress);

            return foundAddress;
        }
    }
}
