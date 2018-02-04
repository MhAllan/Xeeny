using Xeeny.Api.Base;
using Xeeny.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Api.Server
{
    public abstract class BaseServiceHostBuilder : BaseBuilder
    {
        internal protected abstract List<IListener> Listeners { get; set; }
        internal protected abstract Type CallbackType { get; set; }
    }
}
