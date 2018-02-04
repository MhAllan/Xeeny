using Xeeny.Connections;
using Xeeny.Descriptions;
using Xeeny.Proxies.ILGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Xeeny.Proxies.ProxyGeneration
{
    class ClientProxyEmitter<TContract> : DecorationEmitter
    {
        static readonly Type _clientType = typeof(ClientConnection);

        public ClientProxyEmitter(FieldBuilder decoratedField) : base(decoratedField)
        {

        }

        protected override string CreateMethodName(Type interfaceType, MethodInfo method)
        {
            return new OperationDescription(method).Operation;
        }

        protected override void EmitBody(MethodBuilder methodBuilder, MethodInfo method, FieldBuilder decoratedField)
        {
            var ilGenerator = methodBuilder.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, decoratedField);

            var operation = new OperationDescription(method);

            MethodInfo methodInfo = MapOperationToInnerProxyMethod(operation);

            //emit operation
            ilGenerator.Emit(OpCodes.Ldstr, methodBuilder.Name);

            //emit parameters
            var parameterTypes = operation.ParameterTypes;
            ilGenerator.Emit(OpCodes.Ldc_I4_S, parameterTypes.Length);
            ilGenerator.Emit(OpCodes.Newarr, typeof(object));

            for (byte x = 0; x < parameterTypes.Length; x++)
            {
                var xType = parameterTypes[x];
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Ldc_I4_S, x);
                switch (x)
                {
                    case 0: ilGenerator.Emit(OpCodes.Ldarg_1); break;
                    case 1: ilGenerator.Emit(OpCodes.Ldarg_2); break;
                    case 2: ilGenerator.Emit(OpCodes.Ldarg_3); break;
                    default: ilGenerator.Emit(OpCodes.Ldarg_S, x + 1); break;
                }
                if (xType.IsValueType)
                    ilGenerator.Emit(OpCodes.Box, xType);
                ilGenerator.Emit(OpCodes.Stelem_Ref);
            }

            ilGenerator.Emit(OpCodes.Call, methodInfo);
            ilGenerator.Emit(OpCodes.Ret);
        }

        protected virtual MethodInfo MapOperationToInnerProxyMethod(OperationDescription operation)
        {
            var invokationType = operation.InvokationType;
            switch (invokationType)
            {
                case OperationInvokationType.OneWay:
                    {
                        return _clientType.GetMethod(nameof(ClientConnection.SendOneWay));
                    }
                case OperationInvokationType.Void:
                    {
                        return _clientType.GetMethod(nameof(ClientConnection.SendAndWait));
                    }
                case OperationInvokationType.Return:
                    {
                        return _clientType.GetMethod(nameof(ClientConnection.Invoke))
                                            .MakeGenericMethod(operation.ReturnType);
                    }
                case OperationInvokationType.AwaitableOneWay:
                    {
                        return _clientType.GetMethod(nameof(ClientConnection.SendOneWayAsync));
                    }
                case OperationInvokationType.AwaitableVoid:
                    {
                        return _clientType.GetMethod(nameof(ClientConnection.SendAndWaitAsync));
                    }
                case OperationInvokationType.AwaitableReturn:
                    {
                        return _clientType.GetMethod(nameof(ClientConnection.InvokeAsync))
                                            .MakeGenericMethod(operation.ReturnType.GetGenericArguments()
                                            .First());
                    }
                default:
                    {
                        throw new NotSupportedException(invokationType.ToString());
                    }
            }
        }
    }
}
