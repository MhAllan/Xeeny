using Xeeny.Connections;
using Xeeny.Dispatching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.ChatExample
{
    public class ChatService : IChatService
    {
        ConcurrentDictionary<string, ICallback> _clients = new ConcurrentDictionary<string, ICallback>();

        ICallback GetCaller() => OperationContext.Current.GetCallback<ICallback>();

        //return Task is better, this is just for example
        public void Join(string id)
        {
            var caller = GetCaller();
            _clients.AddOrUpdate(id, caller, (k, v) => caller);
            ((IConnection)caller).SessionEnded += s =>
            {
                _clients.TryRemove(id, out ICallback _);
            };
        }

        public Task Say(string message)
        {
            foreach(var client in _clients.Values)
            {
                client.OnServerUpdates(message);
            }

            return Task.CompletedTask;
        }

        public Task TellOthers(string message)
        {
            var caller = GetCaller();
            var others = _clients.Where(x => x.Value != caller);
            foreach(var other in others)
            {
                var client = other.Value;
                client.OnServerUpdates(message);
            }

            return Task.CompletedTask;

        }

        public Task PrivateTo(string id, string message)
        {
            _clients[id].OnServerUpdates(message);

            return Task.CompletedTask;
        }

        public Task WhoIsOnline()
        {
            var online = _clients.Keys.ToArray();
            foreach(var client in _clients.Values)
            {
                client.WhoIsOnline(online);
            }

            return Task.CompletedTask;
        }
    }
}
