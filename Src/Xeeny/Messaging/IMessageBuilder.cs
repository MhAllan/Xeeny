using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Transports;

namespace Xeeny.Messaging
{
    interface IMessageBuilder
    {
        Message CreateOneWayRequest(string operation, object[] parameters);
        Message CreateRequest(string operation, object[] parameters);
        Message CreateResponse(Guid id, object response);
        Message CreateError(Guid id, string error);

        string UnpackAddress(Message message, out ArraySegment<byte> parameters);
        object[] UnpackParameters(ArraySegment<byte> parameters, Type[] parameterTypes);

        TResp UnpackResponse<TResp>(byte[] payload);
    }
}
