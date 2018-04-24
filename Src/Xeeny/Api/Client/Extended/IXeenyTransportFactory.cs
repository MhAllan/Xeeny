using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports;

namespace Xeeny.Api.Client.Extended
{
    public interface IXeenyTransportFactory
    {
        TransportBase CreateTransport();
    }
}
