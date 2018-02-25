using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Sockets.Protocol.Messages;

namespace Xeeny.Dispatching
{
    readonly struct RequestHandleResult
    {
        public bool HasResponse => _hasMessage;
        public Message Response => _response;

        readonly Message _response;
        readonly bool _hasMessage;
        public RequestHandleResult(Message response, bool hasMessage)
        {
            _response = response;
            _hasMessage = hasMessage;
        }
    }
}
