using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports.Channels
{
    public class PipelineTransportChannel<T> : PipelineTransportChannel where T: TransportChannel
    {
        new protected T NextChannel { get; }

        public PipelineTransportChannel(T nextChannel) : base(nextChannel)
        {
            NextChannel = nextChannel;
        }
    }
}
