using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Xeeny.Descriptions;
using Xeeny.Messaging;
using Xeeny.Connections;
using Microsoft.Extensions.Logging;
using Xeeny.Transports;

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

        public async Task<RequestHandleResult> HandleRequest(Message message, ServerConnection serverProxy)
        {
            try
            {
                var operation = _msgBuilder.UnpackAddress(message, out ArraySegment<byte> parametersSpan);
                var operationContext = CreateOperationContext(serverProxy, operation);
                var desc = operationContext.OperationDescription;

                var parameters = _msgBuilder.UnpackParameters(parametersSpan, desc.ParameterTypes);
                var result = await operationContext.Execute(_service, parameters);

                if (!desc.IsOneWay)
                {
                    var response = _msgBuilder.CreateResponse(message.Id, result);
                    return new RequestHandleResult(response, true);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"{nameof(InstanceContext<TService>)} failed to handle request");

                if (message.MessageType != MessageType.OneWayRequest)
                {
                    var error = _msgBuilder.CreateResponse(message.Id, ex.Message);
                    return new RequestHandleResult(error, true);
                }
            }

            return new RequestHandleResult(default, false);
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
