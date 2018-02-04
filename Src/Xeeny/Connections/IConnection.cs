using Xeeny.Sockets;
using System;
using System.Text;

namespace Xeeny.Connections
{
    public interface IConnection : IConnectionObject, IConnectionSession, IDisposable
    {
        
    }
}
