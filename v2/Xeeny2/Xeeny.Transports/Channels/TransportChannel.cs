using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports.Connections;

namespace Xeeny.Transports.Channels
{
    public abstract class TransportChannel
    {
        public virtual string ConnectionId { get; internal set; }
        public virtual string ConnectionName { get; internal set; }
        public virtual ILoggerFactory LoggerFactory { get; internal set; }

        ILogger _logger;
        public virtual ILogger Logger
        {
            get
            {
                if (_logger == null)
                    _logger = LoggerFactory.CreateLogger(this.GetType());
                return _logger;
            }
        }

        public ConnectionSide ConnectionSide { get; }

        public TransportChannel(ConnectionSide connectionSide)
        {
            ConnectionSide = connectionSide;
        }

        public abstract Task Send(ArraySegment<byte> data, CancellationToken ct);

        public abstract Task Connect(CancellationToken ct);

        public abstract Task<int> Receive(ArraySegment<byte> buffer, CancellationToken ct);

        public abstract Task Close(CancellationToken ct);
    }
}
