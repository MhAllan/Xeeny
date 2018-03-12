using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Transports
{
    public interface IConnectionObject : IDisposable
    {
        event Action<IConnectionObject> StateChanged;
        ConnectionState State { get; }

        string ConnectionId { get; }
        string ConnectionName { get; }

        Task Connect();
        void Close();
    }
}
