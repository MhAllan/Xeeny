using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Xeeny.Proxies.ILGeneration
{
    interface IInterfaceEmitter
    {
        void Emit(TypeBuilder builder, Type interfaceType);
    }
}
