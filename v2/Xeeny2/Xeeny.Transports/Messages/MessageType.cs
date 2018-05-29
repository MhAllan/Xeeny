using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports.Messages
{
    public enum MessageType
    {
        Connect,
        Negotiate,
        KeepAlive,
        Request,
        Response,
        Error,
        Close
    }
}
