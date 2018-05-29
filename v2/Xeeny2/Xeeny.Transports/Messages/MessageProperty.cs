using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports.Messages
{
    public enum MessageProperty : ushort
    {
        //global
        MessageId,
        MessageLength,

        //request-response
        MessageType = 100,
        MethodName,
        IsOneWayRequest,
        Encoding = 200,
        
        CorrelationId = 1000,
        SessionId,
        ReplyTo,
    }
}
