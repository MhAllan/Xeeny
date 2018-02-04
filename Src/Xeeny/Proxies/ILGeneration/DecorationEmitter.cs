using Xeeny.Descriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Xeeny.Proxies.ILGeneration
{
    class DecorationEmitter : IInterfaceEmitter
    {
        readonly FieldBuilder _decoratedField;

        public DecorationEmitter(FieldBuilder decoratedField)
        {
            _decoratedField = decoratedField;
        }

        public void Emit(TypeBuilder builder, Type interfaceType)
        {
            var isNotImplemented = builder.FindInterfaces((t, o) => t == interfaceType, null).Length == 0;

            if (isNotImplemented)
            {
                builder.AddInterfaceImplementation(interfaceType);

                var methods = interfaceType.GetMethods();

                foreach (var method in methods)
                {
                    var name = CreateMethodName(interfaceType, method);
                    var methodBuilder = builder.DefineMethod(
                               name,
                               MethodAttributes.Public | MethodAttributes.Virtual,
                               method.ReturnType,
                               method.GetParameters().Select(x => x.ParameterType).ToArray());

                    EmitBody(methodBuilder, method, _decoratedField);

                    builder.DefineMethodOverride(methodBuilder, method);
                }
            }
        }

        protected virtual string CreateMethodName(Type interfaceType, MethodInfo method)
        {
            return $"{interfaceType.Name}.{method.Name}";
        }

        protected virtual void EmitBody(MethodBuilder methodBuilder, MethodInfo method, FieldBuilder decoratedField)
        {
            var ilGenerator = methodBuilder.GetILGenerator();

            var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
            var decoratedMethod = decoratedField.FieldType.GetMethod(method.Name, parameterTypes);

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, decoratedField);

            for (byte x = 0; x < parameterTypes.Length; x++)
            {
                switch (x)
                {
                    case 0: ilGenerator.Emit(OpCodes.Ldarg_1); break;
                    case 1: ilGenerator.Emit(OpCodes.Ldarg_2); break;
                    case 2: ilGenerator.Emit(OpCodes.Ldarg_3); break;
                    default: ilGenerator.Emit(OpCodes.Ldarg_S, x + 1); break;
                }
            }

            ilGenerator.Emit(OpCodes.Call, decoratedMethod);
            ilGenerator.Emit(OpCodes.Ret);
        }
    }
}
