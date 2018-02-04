using Xeeny.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Sockets
{
    public interface IListener
    {
        void Listen();
        Task<ISocket> AcceptSocket();
        void Close();
    }
}
