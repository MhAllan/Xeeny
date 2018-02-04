using Xeeny.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Api.Client.Extended
{
    public interface IXeenySocketFactory
    {
        SocketBase CreateSocket();
    }
}
