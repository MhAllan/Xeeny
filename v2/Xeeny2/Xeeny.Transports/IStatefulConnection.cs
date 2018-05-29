using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports
{
    public delegate void ConnectionStateChanged(IStatefulConnection connection);

    public interface IStatefulConnection
    {
        event ConnectionStateChanged StateChanged;
        ConnectionState State { get; }

        string ConnectionId { get; }
        string ConnectionName { get; }
        ConnectionSide ConnectionSide { get; }

        Task Connect();
        void Close();
    }
}
