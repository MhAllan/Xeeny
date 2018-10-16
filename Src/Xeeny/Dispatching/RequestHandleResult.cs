using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports.Messages;

namespace Xeeny.Dispatching
{
    readonly struct RequestHandleResult
    {
        public bool HasResponse { get; }
        public Message Response { get; }

        public RequestHandleResult(Message response, bool hasMessage)
        {
            Response = response;
            HasResponse = hasMessage;
        }
    }
}
