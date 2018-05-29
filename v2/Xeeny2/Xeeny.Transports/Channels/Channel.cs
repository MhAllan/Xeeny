using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports.Channels
{
    public abstract class Channel
    {
        public virtual string ConnectionId { get; internal set; }
        public virtual string ConnectionName { get; internal set; }
        public ConnectionSide ConnectionSide { get; }

        public Channel(ConnectionSide connectionSide)
        {
            ConnectionSide = connectionSide;
        }

        public abstract Task Send(ArraySegment<byte> data, CancellationToken ct);

        public abstract Task Connect(CancellationToken ct);

        public abstract Task<int> Receive(ArraySegment<byte> buffer, CancellationToken ct);

        public abstract Task Close(CancellationToken ct);
    }
}
