using Xeeny.Connections;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Dispatching
{
    public interface IConnectionProvider
    {
        IConnection GetConnection();
    }
}
