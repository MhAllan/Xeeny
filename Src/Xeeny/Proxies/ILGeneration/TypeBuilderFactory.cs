using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Xeeny.Proxies.ILGeneration
{
    static class TypeBuilderFactory
    {
        public static TypeBuilder CreateBuilder(string assemblyName, string _namespace, string typeName)
        {
            var an = new AssemblyName(assemblyName);
            var asm = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

            var moduleName = Path.ChangeExtension(an.Name, "dll");
            var module = asm.DefineDynamicModule(moduleName);

            if (!string.IsNullOrEmpty(_namespace))
                _namespace += ".";

            typeName = $"{_namespace}{typeName}";

            var builder = module.DefineType(typeName,
                TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit
                | TypeAttributes.AutoClass | TypeAttributes.NotPublic | TypeAttributes.Sealed);

            return builder;
        }
    }
}
