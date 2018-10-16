using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xeeny.Transports;
using Xeeny.Transports.Messages;
using Xeeny.Transports.Connections;

namespace Xeeny.Sockets.TcpTransports
{
    class TcpTransport : DecoratorTransport
    {
        bool IsConcurrent => Transport is ConcurrentStreamTransport;

        public TcpTransport(MessageTransport transport)
            : base(transport)
        {
        }

        protected override async void OnStateChanged(IConnection connection)
        {
            if (connection.State == ConnectionState.Connected && ConnectionSide == ConnectionSide.Client && IsConcurrent)
            {
                var msg = Message.CreateNegotiateMsg();
                msg.Properties.Add("MessageChannel:IsConcurrent", true.ToString());

                await SendMessage(msg);
            }

            base.OnStateChanged(connection);
        }

        protected override async void OnNegotiateMessageReceived(ITransport transport, Message msg)
        {
            var isRemoteConcurrent = bool.Parse(msg.Properties["MessageChannel:IsConcurrent"]);

            if (isRemoteConcurrent != IsConcurrent)
            {
                if (isRemoteConcurrent)
                {
                    var bufferedTransport = (MessageTransport)Transport;
                    Transport = new ConcurrentStreamTransport(Transport.Channel, bufferedTransport.Settings, Transport.LoggerFactory);

                    if (ConnectionSide == ConnectionSide.Server)
                    {
                        var reply = Message.CreateNegotiateMsg();
                        reply.Properties["MessageChannel:IsConcurrent"] = true.ToString();

                        await SendMessage(reply);
                    }
                }
            }
        }
    }
}
