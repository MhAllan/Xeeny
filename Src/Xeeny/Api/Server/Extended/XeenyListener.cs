using Xeeny.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Api.Server.Extended
{
    public abstract class XeenyListener : IListener
    {
        protected abstract Task<SocketBase> AcceptXeenySocket();

        public async Task<ISocket> AcceptSocket()
        {
            return await AcceptXeenySocket();
        }

        public abstract void Close();

        public abstract void Listen();
    }
}
