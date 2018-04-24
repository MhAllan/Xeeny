using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Xeeny.Api.Client.Extended;
using Xeeny.Sockets;
using Xeeny.Transports;

namespace Xeeny.Api.Client
{
    class TransportFactory : ITransportFactory
    {
        readonly IXeenyTransportFactory _customFactory;
        readonly string _address;
        readonly SocketType _socketType;
        readonly TransportSettings _settings;

        public TransportFactory(string address, SocketType socketType, TransportSettings settings)
        {
            _address = address;
            _socketType = socketType;
            _settings = settings;
        }

        public TransportFactory(IXeenyTransportFactory customFactory)
        {
            _customFactory = customFactory;
        }

        public ITransport CreateTransport(ILoggerFactory loggerFactory)
        {
            if (_customFactory != null)
                return _customFactory.CreateTransport();

            if (_socketType == SocketType.TCP)
            {
                return SocketTools.CreateTcpTransport(_address, (SocketTransportSettings)_settings, loggerFactory);
            }
            else
            {
                throw new NotSupportedException(_socketType.ToString());
            }
        }
    }
}
