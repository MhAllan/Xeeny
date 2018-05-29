using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports
{
    public interface IConnectionObject
    {
        string ConnectionId { get; }
        string ConnectionName { get; }
        ConnectionSide ConnectionSide { get; }

        Task Connect(CancellationToken ct);
        Task Close(CancellationToken ct);
    }
}
