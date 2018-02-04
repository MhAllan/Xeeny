using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Xeeny.Descriptions;
using Xeeny.Sockets.Messages;
using Xeeny.Messaging;
using Xeeny.Connections;
using Microsoft.Extensions.Logging;

namespace Xeeny.Dispatching
{
    sealed class InstanceContext<TService> : IInstanceContext
    {
        public static readonly IEnumerable<OperationDescription> operations = TypeDescription<TService>.Operations;

        public object Service => _service;

        readonly IMessageBuilder _msgBuilder;
        readonly TService _service;
        readonly ILogger _logger;

        public InstanceContext(TService instance, IMessageBuilder msgBuilder, ILoggerFactory loggerFactory)
        {
            _service = instance;
            _msgBuilder = msgBuilder;
            _logger = loggerFactory.CreateLogger("InstanceContext");
        }

        public async Task<Message?> HandleRequest(Message message, ServerConnection serverProxy)
        {
            try
            {
                var operation = _msgBuilder.UnpackAddress(message);
                var operationContext = CreateOperationContext(serverProxy, operation);
                var parameters = _msgBuilder.UnpackParameters(message, operationContext.OperationDescription.ParameterTypes);

                var result = await operationContext.Execute(_service, parameters);

                if (!operationContext.OperationDescription.IsOneWay)
                {
                    return _msgBuilder.CreateResponse(message.Id, result);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"{nameof(InstanceContext<TService>)} failed to handle request");

                if (message.MessageType != MessageType.OneWayRequest)
                {
                    return _msgBuilder.CreateResponse(message.Id, ex.Message);
                }
            }

            return null;
        }

        public OperationContext CreateOperationContext(ServerConnection serverProxy, string operation)
        {
            var operationDescription = operations.FirstOrDefault(x => x.Operation == operation);
            if (operationDescription == null)
            {
                throw new Exception($"Could not find operation {operation}, operation {operation}");
            }
            return new OperationContext(operationDescription, serverProxy);
        }
    }
}
