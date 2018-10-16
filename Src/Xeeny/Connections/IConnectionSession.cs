using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Connections
{
    public delegate void SessionEnded(IConnectionSession connection);

    public interface IConnectionSession
    {
        event SessionEnded SessionEnded;
    }
}
