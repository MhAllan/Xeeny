using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports.Channels
{
    public abstract class PipelineChannel : Channel
    {
        public override string ConnectionId
        {
            get => _next.ConnectionId;
            internal set => _next.ConnectionId = value;
        }
        public override string ConnectionName
        {
            get => _next.ConnectionName;
            internal set => _next.ConnectionName = value;
        }

        readonly Channel _next;

        public PipelineChannel(Channel next) : base(next.ConnectionSide)
        {
            _next = next;
        }

        public override async Task Connect(CancellationToken ct)
        {
            await NextConnect(ct);
        }

        public override async Task Send(ArraySegment<byte> data, CancellationToken ct)
        {
            await NextSend(data, ct);
        }

        public override Task<int> Receive(ArraySegment<byte> buffer, CancellationToken ct)
        {
            return NextReceive(buffer, ct);
        }

        public override async Task Close(CancellationToken ct)
        {
            await NextClose(ct);
        }

        protected async Task NextConnect(CancellationToken ct)
        {
            if (_next != null)
            {
                await _next.Connect(ct);
            }
        }

        protected async Task NextSend(ArraySegment<byte> data, CancellationToken ct)
        {
            if (_next != null)
            {
                await _next.Send(data, ct);
            }
        }

        protected async Task<int> NextReceive(ArraySegment<byte> buffer, CancellationToken ct)
        {
            if (_next == null)
            {
                return 0;
            }

            return await _next.Receive(buffer, ct);
        }

        protected async Task NextClose(CancellationToken ct)
        {
            if (_next != null)
            {
                await _next.Close(ct);
            }
        }
    }
}
