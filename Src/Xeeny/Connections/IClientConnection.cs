using Xeeny.Connections;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Connections
{
    public interface IClientConnection : IConnection
    {
        void SendOneWay(string operation, params object[] parameters);
        void SendAndWait(string operation, params object[] parameters);
        T Invoke<T>(string operation, params object[] parameters);

        Task SendOneWayAsync(string operation, params object[] parameters);
        Task SendAndWaitAsync(string operation, params object[] parameters);
        Task<T> InvokeAsync<T>(string operation, params object[] parameters);
    }
}
