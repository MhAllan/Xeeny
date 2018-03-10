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

        internal object Execute(object service, object[] parameters)
        {
            _current.Value = _serverProxy;
            var invokationType = this.OperationDescription.InvokationType;
            var method = OperationDescription.MethodInfo;
            var result = method.Invoke(service, parameters);
            return result;
        }
    }
}
