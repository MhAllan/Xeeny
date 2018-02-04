using Xeeny.Attributes;
using Xeeny.Connections;
using Xeeny.Descriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Xeeny.Proxies.ProxyGeneration
{
    class ServerProxyEmitter<TContract> : ClientProxyEmitter<TContract>
    {
        static readonly Type _serverProxyType = typeof(ServerConnection);

        public ServerProxyEmitter(FieldBuilder delegatedField) : base(delegatedField)
        {

        }

        protected override MethodInfo MapOperationToInnerProxyMethod(OperationDescription operation)
        {
            var invokationType = operation.InvokationType;
            switch (invokationType)
            {
                case OperationInvokationType.OneWay:
                    {
                        return _serverProxyType.GetMethod(nameof(ServerConnection.SendOneWay));
                    }
                case OperationInvokationType.Void:
                    {
                        return _serverProxyType.GetMethod(nameof(ServerConnection.SendOneWay));
                    }
                case OperationInvokationType.AwaitableOneWay:
                    {
                        return _serverProxyType.GetMethod(nameof(ServerConnection.SendOneWayAsync));
                    }
                case OperationInvokationType.AwaitableVoid:
                    {
                        return _serverProxyType.GetMethod(nameof(ServerConnection.SendOneWayAsync));
                    }
                default:
                    {
                        throw new NotSupportedException(invokationType.ToString());
                    }
            }
        }
    }
}
