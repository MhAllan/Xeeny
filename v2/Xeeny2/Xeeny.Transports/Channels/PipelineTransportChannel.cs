using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Xeeny.Transports.Channels
{
    public abstract class PipelineTransportChannel : TransportChannel
    {
        public override string ConnectionId
        {
            get => NextChannel.ConnectionId;
            internal set => NextChannel.ConnectionId = value;
        }
        public override string ConnectionName
        {
            get => NextChannel.ConnectionName;
            internal set => NextChannel.ConnectionName = value;
        }
        public override ILoggerFactory LoggerFactory
        {
            get => NextChannel.LoggerFactory;
            internal set => NextChannel.LoggerFactory = value;
        }

        protected TransportChannel NextChannel;

        public PipelineTransportChannel(TransportChannel nextChannel) : base(nextChannel.ConnectionSide)
        {
            NextChannel = nextChannel;
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
            if (NextChannel != null)
            {
                await NextChannel.Connect(ct);
            }
        }

        protected async Task NextSend(ArraySegment<byte> data, CancellationToken ct)
        {
            if (NextChannel != null)
            {
                await NextChannel.Send(data, ct);
            }
        }

        protected async Task<int> NextReceive(ArraySegment<byte> buffer, CancellationToken ct)
        {
            if (NextChannel == null)
            {
                return 0;
            }

            return await NextChannel.Receive(buffer, ct);
        }

        protected async Task NextClose(CancellationToken ct)
        {
            if (NextChannel != null)
            {
                await NextChannel.Close(ct);
            }
        }
    }
}
