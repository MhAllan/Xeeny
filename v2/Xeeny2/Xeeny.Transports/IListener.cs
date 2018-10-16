using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Transports
{
    public interface IListener
    {
        void Listen();
        Task<ITransport> AcceptConnection();
        void Close();
    }
}
