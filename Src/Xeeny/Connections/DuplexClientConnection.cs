using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Dispatching;
using Xeeny.Messaging;
using Xeeny.Sockets;
using Xeeny.Sockets.Protocol.Messages;

namespace Xeeny.Connections
{
    public class DuplexClientConnection : ClientConnection
    {
        readonly IInstanceContextFactory _instanceContextFactory;

        internal DuplexClientConnection(ISocket socket, IMessageBuilder msgBuilder,
            IInstanceContextFactory instanceContextFactory)

            : base(socket, msgBuilder)
        {
            _instanceContextFactory = instanceContextFactory;
        }

        protected override async void OnRequestReceived(ISocket socket, Message message)
        {
            var instanceContext = _instanceContextFactory.CreateInstanceContext(this);

            await instanceContext.HandleRequest(message, null);
        }
    }
}
