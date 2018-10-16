using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports.Messages
{
    public enum MessageType
    {
        Empty,
        Connect,
        Negotiate,
        KeepAlive,
        OneWayRequest,
        Request,
        Response,
        Error,
        Close
    }
}
