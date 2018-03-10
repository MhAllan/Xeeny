using Xeeny.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Transports;

namespace Xeeny.Api.Server.Extended
{
    public abstract class XeenyListener : IListener
    {
        protected abstract Task<TransportBase> AcceptXeenySocket();

        public async Task<ITransport> AcceptSocket()
        {
            return await AcceptXeenySocket();
        }

        public abstract void Close();

        public abstract void Listen();
    }
}
