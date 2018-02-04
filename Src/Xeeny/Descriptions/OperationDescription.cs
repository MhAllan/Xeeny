using Xeeny.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Descriptions
{
    class OperationDescription
    {
        public readonly string Operation;
        public readonly bool IsOneWay;
        public readonly bool IsAwaitable;
        public readonly bool HasReturn;
        public readonly OperationInvokationType InvokationType;

        public readonly MethodInfo MethodInfo;
        public readonly Type ReturnType;
        public readonly Type[] ParameterTypes;

        public OperationDescription(MethodInfo methodInfo)
        {
            if (methodInfo.IsGenericMethod)
                throw new Exception($"Method {methodInfo.Name} can not be generic");

            if (methodInfo.ReturnType.IsInterface)
                throw new Exception($"Mthod {methodInfo.Name} can not return Interface");

            var isOneWay = false;
            var operation = $"{methodInfo.DeclaringType.Name}.{methodInfo.Name}";
            var operationAttribute = methodInfo.GetCustomAttribute<OperationAttribute>();
            if (operationAttribute != null)
            {
                if (!string.IsNullOrEmpty(operationAttribute.Name))
                {
                    operation = operationAttribute.Name;
                }
                isOneWay = operationAttribute.IsOneWay;
            }

            var isVoid = methodInfo.ReturnType == typeof(void);
            if (isVoid)
            {
                if (isOneWay)
                {
                    InvokationType = OperationInvokationType.OneWay;
                }
                else
                {
                    InvokationType = OperationInvokationType.Void;
                }
            }
            else
            {
                var isVoidTask = methodInfo.ReturnType == typeof(Task);
                if (isVoidTask)
                {
                    if (isOneWay)
                    {
                        InvokationType = OperationInvokationType.AwaitableOneWay;
                    }
                    else
                    {
                        InvokationType = OperationInvokationType.AwaitableVoid;
                    }
                }
                else if (isOneWay)
                {
                    throw new Exception($"{operation} is [OneWay], it must return Void or Task");
                }
                else
                {
                    var isAwaitable = methodInfo.ReturnType.IsGenericType &&
                                        methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);

                    if (isAwaitable)
                    {
                        InvokationType = OperationInvokationType.AwaitableReturn;
                    }
                    else
                    {
                        InvokationType = OperationInvokationType.Return;
                    }
                }
            }

            var parameters = methodInfo.GetParameters();
            foreach (var p in parameters)
            {
                if (p.IsOut || p.ParameterType.IsByRef || p.ParameterType.IsMarshalByRef || p.ParameterType.IsPointer
                    || p.ParameterType.IsInterface || p.ParameterType.IsAbstract || p.ParameterType.IsNotPublic)
                {
                    throw new Exception($"Parameter {p.Name} of method {methodInfo.Name} is not valid, " +
                        $"Valid parameters can not be: ByRef, Interface, abstract, Pointers, MarshalByRef, " +
                        $"or not public");
                }
            }

            Operation = operation;
            IsOneWay = isOneWay;
            IsAwaitable = InvokationType == OperationInvokationType.AwaitableOneWay ||
                            InvokationType == OperationInvokationType.AwaitableVoid ||
                            InvokationType == OperationInvokationType.AwaitableReturn;

            HasReturn = InvokationType == OperationInvokationType.Return ||
                        InvokationType == OperationInvokationType.AwaitableReturn;

            MethodInfo = methodInfo;
            ReturnType = methodInfo.ReturnType;
            ParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
        }
    }
}
