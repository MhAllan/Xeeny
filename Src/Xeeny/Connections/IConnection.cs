using System;
using System.Text;

namespace Xeeny.Connections
{
    public interface IConnection : Transports.Connections.IConnection, IConnectionSession
    {
        
    }
}
