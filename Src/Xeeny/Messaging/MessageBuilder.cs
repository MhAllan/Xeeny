using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xeeny.Serialization;
using Xeeny.Sockets.Protocol.Messages;

namespace Xeeny.Messaging
{
    class MessageBuilder : IMessageBuilder
    {
        readonly ISerializer _serializer;
        public MessageBuilder(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public Message CreateOneWayRequest(string operation, object[] parameters)
        {
            var payload = PackParametersWithAddress(operation, parameters);

            return new Message(MessageType.OneWayRequest, Guid.NewGuid(), payload);
        }

        public Message CreateRequest(string operation, object[] parameters)
        {
            var payload = PackParametersWithAddress(operation, parameters);

            return new Message(MessageType.Request, Guid.NewGuid(), payload);
        }


        public Message CreateResponse(Guid id, object response)
        {
            var payload = PackParameters(response);
            return new Message(MessageType.Response, id, payload);
        }

        public Message CreateError(Guid id, string error)
        {
            var payload = PackParameters(error);
            return new Message(MessageType.Error, id, payload);
        }

        public string UnpackAddress(Message message, out ArraySegment<byte> parameters)
        {
            var buffer = message.Payload;
            var size = BitConverter.ToInt32(buffer, 0);
            var address = Encoding.ASCII.GetString(buffer, 4, size);
            var addressLen = address.Length + 4;
            parameters = new ArraySegment<byte>(buffer, addressLen, buffer.Length - addressLen);

            return address;
        }

        public object[] UnpackParameters(ArraySegment<byte> buffer, Type[] parameterTypes)
        {
            object[] parameters = null;
            if (parameterTypes != null && parameterTypes.Any())
            {
                var length = parameterTypes.Length;
                var messageParams = _serializer.Deserialize<byte[][]>(buffer.ToArray());
                parameters = new object[length];

                for (int i = 0; i < length; i++)
                {
                    var pt = parameterTypes[i];
                    if (pt == typeof(byte[]))
                        parameters[i] = messageParams[i];
                    else
                        parameters[i] = _serializer.Deserialize(pt, messageParams[i]);
                }
            }

            return parameters;
        }

        public TResponse UnpackResponse<TResponse>(byte[] payload)
        {
            var parameters = _serializer.Deserialize<byte[][]>(payload);
            return _serializer.Deserialize<TResponse>(parameters[0]);
        }

        byte[] PackParametersWithAddress(string address, params object[] parameters)
        {
            var addressSize = BitConverter.GetBytes(address.Length);
            var addressBytes = Encoding.ASCII.GetBytes(address);
            var parametersPayload = PackParameters(parameters);
            var len = parametersPayload == null ? 0 : parametersPayload.Length;
            len += address.Length + 4;
            var array = new byte[len];

            Array.Copy(addressSize, array, 4);
            Array.Copy(addressBytes, 0, array, 4, address.Length);
            
            if(parametersPayload != null)
            {
                Array.Copy(parametersPayload, 0, array, address.Length + 4, parametersPayload.Length);
            }

            return array;
        }

        byte[] PackParameters(params object[] parameters)
        {
            byte[] result = null;

            if (parameters != null)
            {
                var length = parameters.Length;
                if (length > 0)
                {
                    var array = new byte[length][];

                    for (int i = 0; i < length; i++)
                    {
                        var p = parameters[i];
                        if (p is byte[])
                            array[i] = (byte[])p;
                        else
                            array[i] = _serializer.Serialize(p);
                    }

                    result = _serializer.Serialize(array);
                }
            }

            return result;
        }
    }
}
