using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets.Messages
{
    public enum MessageType : byte
    {
        Ping,
        Connect,
        OneWayRequest,
        Request,
        Response,
        Error,
        Close
    }
}
