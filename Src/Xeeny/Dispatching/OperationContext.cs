using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Descriptions;
namespace Xeeny.Dispatching
{
    public sealed class OperationContext
    {
        static readonly ThreadLocal<ICallbackGenerator> _current = new ThreadLocal<ICallbackGenerator>();

        public static ICallbackGenerator Current => _current.Value;

        internal readonly OperationDescription OperationDescription;

        readonly ICallbackGenerator _serverProxy;

        internal OperationContext(OperationDescription operation, ICallbackGenerator serverProxy)
        {
            this.OperationDescription = operation;
            _serverProxy = serverProxy;
        }

        internal async Task<object> Execute(object service, object[] parameters)
        {
            _current.Value = _serverProxy;

            var invokationType = this.OperationDescription.InvokationType;
            switch (invokationType)
            {
                case OperationInvokationType.OneWay:
                    {
                        this.OperationDescription.MethodInfo.Invoke(service, parameters);
                        return 1;
                    }
                case OperationInvokationType.Void:
                    {
                        this.OperationDescription.MethodInfo.Invoke(service, parameters);
                        return 1;
                    }
                case OperationInvokationType.Return:
                    {
                        return this.OperationDescription.MethodInfo.Invoke(service, parameters);
                    }
                case OperationInvokationType.AwaitableOneWay:
                    {
                        this.OperationDescription.MethodInfo.Invoke(service, parameters);
                        return 1;
                    }
                case OperationInvokationType.AwaitableVoid:
                    {
                        await (dynamic)this.OperationDescription.MethodInfo.Invoke(service, parameters);
                        return 1;
                    }
                case OperationInvokationType.AwaitableReturn:
                    {
                        return await (dynamic)this.OperationDescription.MethodInfo.Invoke(service, parameters);
                    }
                default: throw new NotSupportedException(invokationType.ToString());
            }
        }
    }
}
