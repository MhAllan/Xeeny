using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports
{
    public enum MessageType : byte
    {
        KeepAlive,
        Agreement,
        Connect,
        OneWayRequest,
        Request,
        Response,
        Error,
        Close
    }
}
