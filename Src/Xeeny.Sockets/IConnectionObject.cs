using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Sockets
{
    public interface IConnectionObject : IDisposable
    {
        event Action<IConnectionObject> StateChanged;
        ConnectionState State { get; }

        Task Connect();
        void Close();
    }
}
