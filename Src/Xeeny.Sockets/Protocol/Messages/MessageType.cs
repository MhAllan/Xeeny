using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets.Protocol.Messages
{
    public enum MessageType : byte
    {
        Ping,
        Agreement,
        Connect,
        OneWayRequest,
        Request,
        Response,
        Error,
        Close
    }
}
