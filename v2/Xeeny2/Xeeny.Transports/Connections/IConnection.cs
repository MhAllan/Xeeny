using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports.Connections
{
    public delegate void ConnectionStateChanged(IConnection connection);

    public interface IConnection : IDisposable
    {
        event ConnectionStateChanged StateChanged;

        ConnectionState State { get; }

        string ConnectionId { get; }
        string ConnectionName { get; }
        ConnectionSide ConnectionSide { get; }

        Task Connect(CancellationToken ct = default);
        Task Close(CancellationToken ct = default);
    }
}
