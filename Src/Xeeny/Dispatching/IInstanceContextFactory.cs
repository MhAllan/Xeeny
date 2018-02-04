using Xeeny.Connections;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Dispatching
{
    interface IInstanceContextFactory
    {
        event Action<IInstanceContextFactory, IInstanceContext> InstanceCreated;
        event Action<IInstanceContextFactory, IInstanceContext> SessionInstanceRemoved;

        IInstanceContext CreateInstanceContext(IConnection connectionSession);
    }
}
