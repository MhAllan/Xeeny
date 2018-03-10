using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Xeeny.Api.Client.Extended;
using Xeeny.Sockets;
using Xeeny.Transports;

namespace Xeeny.Api.Client
{
    class SocketFactory : ISocketFactory
    {
        readonly IXeenySocketFactory _customFactory;
        readonly string _address;
        readonly SocketType _socketType;
        readonly TransportSettings _settings;

        public SocketFactory(string address, SocketType socketType, TransportSettings settings)
        {
            _address = address;
            _socketType = socketType;
            _settings = settings;
        }

        public SocketFactory(IXeenySocketFactory customFactory)
        {
            _customFactory = customFactory;
        }

        public ITransport CreateSocket(ILoggerFactory loggerFactory)
        {
            if (_customFactory != null)
                return _customFactory.CreateSocket();

            if (_socketType == SocketType.WebSocket)
            {
                return SocketTools.CreateWebSocket(_address, _settings, loggerFactory);
            }
            else if (_socketType == SocketType.TCP)
            {
                return SocketTools.CreateTcpSocket(_address, (IPSocketSettings)_settings, loggerFactory);
            }
            else
            {
                throw new NotSupportedException(_socketType.ToString());
            }
        }
    }
}
