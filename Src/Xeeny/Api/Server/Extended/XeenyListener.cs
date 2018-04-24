using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Transports;

namespace Xeeny.Api.Server.Extended
{
    public abstract class XeenyListener : IListener
    {
        protected abstract Task<TransportBase> AcceptXeenyTransport();

        public async Task<ITransport> AcceptConnection()
        {
            return await AcceptXeenyTransport();
        }

        public abstract void Close();

        public abstract void Listen();
    }
}
