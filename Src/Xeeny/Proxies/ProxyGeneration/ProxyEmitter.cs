using Xeeny.Connections;
using Xeeny.Descriptions;
using Xeeny.Proxies.ILGeneration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Xeeny.Proxies.ProxyGeneration
{
    class ProxyEmitter<TInterface, TImplementation>
    {
        static readonly Type _interface = typeof(TInterface);
        static readonly Type _innerProxyType = typeof(TImplementation);

        static Type _emittedType;


        readonly TImplementation _innerProxy;
        TypeBuilder _builder;

        public ProxyEmitter(TImplementation innerProxy)
        {
            _innerProxy = innerProxy;
        }

        public TInterface CreateProxy()
        {
            if (_emittedType == null)
            {
                if (!_interface.IsInterface || !_interface.IsPublic)
                {
                    throw new Exception($"{_interface.FullName} must be public interface");
                }

                _emittedType = Emit();
            }

            var proxy = Activator.CreateInstance(_emittedType, _innerProxy);

            return (TInterface)proxy;
        }

        Type Emit()
        {
            CreateBuilder();

            var innerProxyField = EmitConstructor();

            ImplementInterface(typeof(IConnection), new DecorationEmitter(innerProxyField));

            var interfaceEmitter = GetDelegationProxyEmitter(innerProxyField);

            ImplementInterface(_interface, interfaceEmitter);

            return _builder.CreateTypeInfo().AsType();
        }

        void CreateBuilder()
        {
            var assemblyName = $"DynamicAssembly_{_interface.Name}";
            var ns = _interface.Namespace;
            var typeName = $"{_interface.Name}_DynamicProxy";

            _builder = TypeBuilderFactory.CreateBuilder(assemblyName, ns, typeName);
        }

        FieldBuilder EmitConstructor()
        {
            var innerProxyField = _builder.DefineField("delegatedField", _innerProxyType, FieldAttributes.Private);

            var ctor = _builder.DefineConstructor(MethodAttributes.Public,
                CallingConventions.HasThis,
                new Type[] { innerProxyField.FieldType });

            var il = ctor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, innerProxyField);
            il.Emit(OpCodes.Ret);

            return innerProxyField;
        }

        IInterfaceEmitter GetDelegationProxyEmitter(FieldBuilder innerProxyField)
        {
            if (_innerProxyType == typeof(ClientConnection))
                return new ClientProxyEmitter<TInterface>(innerProxyField);

            if (_innerProxyType == typeof(DuplexClientConnection))
                return new ClientProxyEmitter<TInterface>(innerProxyField);

            if (_innerProxyType == typeof(ServerConnection))
                return new ServerProxyEmitter<TInterface>(innerProxyField);

            throw new NotSupportedException(_innerProxyType.FullName);
        }

        public void ImplementInterface(Type interfaceType, IInterfaceEmitter emitter)
        {
            emitter.Emit(_builder, interfaceType);

            var parentInterfaces = interfaceType.GetInterfaces();
            foreach (var intf in parentInterfaces)
            {
                ImplementInterface(intf, emitter);
            }
        }
    }
}
