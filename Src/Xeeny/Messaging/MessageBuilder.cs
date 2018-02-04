using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xeeny.Serialization;
using Xeeny.Sockets.Messages;

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
            var addr = Encoding.ASCII.GetBytes(operation);
            var payload = PackParameters(parameters);

            return new Message
            {
                MessageType = MessageType.OneWayRequest,
                Address = addr,
                Payload = payload
            };
        }

        public Message CreateRequest(string operation, object[] parameters)
        {
            var addr = Encoding.ASCII.GetBytes(operation);
            var payload = PackParameters(parameters);

            return new Message
            {
                Id = Guid.NewGuid(),
                MessageType = MessageType.Request,
                Address = addr,
                Payload = payload
            };
        }


        public Message CreateResponse(Guid id, object response)
        {
            var payload = PackParameters(response);

            return new Message
            {
                Id = id,
                MessageType = MessageType.Response,
                Payload = payload
            };
        }

        public Message CreateError(Guid id, string error)
        {
            var payload = PackParameters(error);

            return new Message
            {
                Id = id,
                MessageType = MessageType.Error,
                Payload = payload
            };
        }

        public string UnpackAddress(Message message)
        {
            return Encoding.ASCII.GetString(message.Address);
        }

        public object[] UnpackParameters(Message message, Type[] parameterTypes)
        {
            object[] parameters = null;
            if (parameterTypes != null && parameterTypes.Any())
            {
                var length = parameterTypes.Length;
                var messageParams = _serializer.Deserialize<byte[][]>(message.Payload);
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
