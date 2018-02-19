using Xeeny.Connections;
using System;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Sockets.Protocol.Messages;

namespace Xeeny.Dispatching
{
    interface IInstanceContext
    {
        object Service { get; }
        Task<RequestHandleResult> HandleRequest(Message message, ServerConnection serverProxy);
    }
}
