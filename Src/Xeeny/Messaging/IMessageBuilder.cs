using Xeeny.Sockets.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Messaging
{
    interface IMessageBuilder
    {
        Message CreateOneWayRequest(string operation, object[] parameters);
        Message CreateRequest(string operation, object[] parameters);
        Message CreateResponse(Guid id, object response);
        Message CreateError(Guid id, string error);

        string UnpackAddress(Message message);
        object[] UnpackParameters(Message message, Type[] parameterTypes);

        TResp UnpackResponse<TResp>(byte[] payload);
    }
}
