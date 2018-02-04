using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xeeny.Attributes;

namespace Xeeny.Descriptions
{
    class TypeDescription<T>
    {
        static readonly Type _thisType = typeof(T);
        public static readonly IEnumerable<OperationDescription> Operations = GetAllOperations(typeof(T)).ToList();

        static bool _isValidService;
        static bool _isValidContract;
        static bool _isValidCallbackObject;
        //static bool _isValidCallbackInterface;

        public static void ValidateAsService(Type callbackType)
        {
            if (_isValidService)
                return;

            var interfaces = _thisType.GetInterfaces().Where(x => x.IsPublic);
            if (!interfaces.Any())
            {
                throw new Exception($"No public interfaces found for service {_thisType.FullName}");
            }

            ValidateOperations();

            ValidateCallbackInterface(callbackType);

            _isValidService = true;
        }

        public static void ValidateAsContract(Type callbackType)
        {
            if (_isValidContract)
                return;

            ValidateInterface(_thisType);

            ValidateOperations();

            ValidateCallbackInterface(callbackType);

            _isValidContract = true;
        }

        public static void ValidateAsCallbackObject()
        {
            if (_isValidCallbackObject)
                return;

            var interfaces = _thisType.GetInterfaces().Where(x => x.IsPublic);
            if (!interfaces.Any())
            {
                throw new Exception($"No public interfaces found for service {_thisType.FullName}");
            }

            foreach (var intf in interfaces)
            {
                ValidateCallbackInterface(intf);
            }

            _isValidCallbackObject = true;
        }

        //public static void ValidateAsCallbackInterface()
        //{
        //    if (_isValidCallbackInterface)
        //        return;

        //    ValidateCallbackInterface(_thisType);

        //    _isValidCallbackInterface = true;
        //}

        static void ValidateOperations()
        {
            if (!Operations.Any())
            {
                throw new Exception($"No public operations found for service {_thisType.FullName}");
            }
        }

        static void ValidateCallbackInterface(Type callbackType)
        {
            if (callbackType != null)
            {
                ValidateInterface(callbackType);

                var callbackOperations = GetAllOperations(callbackType)
                                                        .Where(x => x.HasReturn == false);
                if (!callbackOperations.Any())
                {
                    throw new Exception($"Callback is defined of type {callbackType.FullName}, but no valid operations, " +
                        $"Valid callbacks must return void or Task");
                }
            }
        }

        static void ValidateInterface(Type type)
        {
            if (!type.IsInterface || !type.IsPublic)
            {
                throw new Exception($"{type.FullName} must be public interface");
            }
        }

        static IEnumerable<OperationDescription> GetAllOperations(Type type)
        {
            if (type.IsInterface)
            {
                var operations = type.GetMethods().Select(x => new OperationDescription(x));
                foreach (var op in operations)
                {
                    yield return op;
                }
            }

            var interfaces = type.GetInterfaces();
            foreach (var intf in interfaces)
            {
                var next = GetAllOperations(intf);
                foreach (var op in next)
                {
                    yield return op;
                }
            }
        }
    }
}
