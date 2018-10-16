using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports.Channels;

namespace Xeeny.Transports
{
    public abstract class MessageTransport : Transport
    {
        public new MessageTransportSettings Settings => (MessageTransportSettings)base.Settings;

        public abstract int MinMessageSize { get; }
        public int MaxMessageSize { get; }
        public int ReceiveBufferSize { get; }
        public int SendBufferSize { get; }

        public MessageTransport(TransportChannel channel, MessageTransportSettings settings, ILoggerFactory loggerFactory)
            : base(channel, settings, loggerFactory)
        {
            if (settings.MaxMessageSize < MinMessageSize)
            {
                throw new Exception($"{nameof(settings.MaxMessageSize)} must be larger than {MinMessageSize}");
            }
            if (settings.SendBufferSize < MinMessageSize)
            {
                throw new Exception($"{nameof(settings.SendBufferSize)} must be larger than {MinMessageSize}");
            }
            if (settings.ReceiveBufferSize < MinMessageSize)
            {
                throw new Exception($"{nameof(settings.ReceiveBufferSize)} must be larger than {MinMessageSize}");
            }
            if (settings.MaxMessageSize < settings.SendBufferSize)
            {
                throw new Exception($"{nameof(settings.MaxMessageSize)} must be larger than {settings.SendBufferSize}");
            }
            if (settings.MaxMessageSize < settings.ReceiveBufferSize)
            {
                throw new Exception($"{nameof(settings.MaxMessageSize)} must be larger than {settings.ReceiveBufferSize}");
            }

            MaxMessageSize = settings.MaxMessageSize;
            ReceiveBufferSize = settings.ReceiveBufferSize;
            SendBufferSize = settings.SendBufferSize;
        }
    }
}
