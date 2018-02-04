using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Connections
{
    public interface IConnectionSession
    {
        event Action<IConnectionSession> SessionEnded;
    }
}
