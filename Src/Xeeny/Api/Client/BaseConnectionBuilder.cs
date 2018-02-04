using Xeeny.Api.Base;
using Xeeny.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Api.Client
{
    public abstract class BaseConnectionBuilder : BaseBuilder
    {
        internal protected abstract ISocketFactory SocketFactory { get; set; }
    }
}
