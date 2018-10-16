﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Dispatching;
using Xeeny.Messaging;
using Xeeny.Transports;
using Xeeny.Transports.Messages;

namespace Xeeny.Connections
{
    public class DuplexClientConnection : ClientConnection
    {
        readonly IInstanceContextFactory _instanceContextFactory;

        internal DuplexClientConnection(ITransport transport, IMessageBuilder msgBuilder,
            IInstanceContextFactory instanceContextFactory)

            : base(transport, msgBuilder)
        {
            _instanceContextFactory = instanceContextFactory;
        }

        protected override async void OnRequestReceived(ITransport transport, Message message)
        {
            var instanceContext = _instanceContextFactory.CreateInstanceContext(this);
            await instanceContext.HandleRequest(message, null);
        }
    }
}
