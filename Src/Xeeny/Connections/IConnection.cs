using System;
using System.Text;
using Xeeny.Transports;

namespace Xeeny.Connections
{
    public interface IConnection : IConnectionObject, IConnectionSession, IDisposable
    {
        
    }
}
