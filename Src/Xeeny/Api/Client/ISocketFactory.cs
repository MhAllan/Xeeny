using Microsoft.Extensions.Logging;
using Xeeny.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Api.Client
{
    public interface ISocketFactory
    {
        ISocket CreateSocket(ILoggerFactory loggerFactory);
    }
}
