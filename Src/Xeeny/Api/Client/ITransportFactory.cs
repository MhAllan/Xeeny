using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports;

namespace Xeeny.Api.Client
{
    public interface ITransportFactory
    {
        ITransport CreateTransport();
    }
}
