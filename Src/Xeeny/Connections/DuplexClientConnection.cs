using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Dispatching;
using Xeeny.Messaging;
using Xeeny.Transports;

namespace Xeeny.Connections
{
    public class DuplexClientConnection : ClientConnection
    {
        readonly IInstanceContextFactory _instanceContextFactory;

        internal DuplexClientConnection(ITransport socket, IMessageBuilder msgBuilder,
            IInstanceContextFactory instanceContextFactory)

            : base(socket, msgBuilder)
        {
            _instanceContextFactory = instanceContextFactory;
        }

        protected override async void OnRequestReceived(ITransport socket, Message message)
        {
            var instanceContext = _instanceContextFactory.CreateInstanceContext(this);

            await instanceContext.HandleRequest(message, null);
        }
    }
}
