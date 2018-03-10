using Xeeny.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Transports;

namespace Xeeny.Sockets
{
    public interface IListener
    {
        void Listen();
        Task<ITransport> AcceptSocket();
        void Close();
    }
}
