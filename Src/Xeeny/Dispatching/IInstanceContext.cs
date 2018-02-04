using Xeeny.Connections;
using Xeeny.Sockets.Messages;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Dispatching
{
    interface IInstanceContext
    {
        object Service { get; }
        Task<Message?> HandleRequest(Message message, ServerConnection serverProxy);
    }
}
