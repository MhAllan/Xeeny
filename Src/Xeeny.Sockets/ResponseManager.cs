using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Sockets.Protocol.Messages;

namespace Xeeny.Sockets
{
    class ResponseManager
    {
        ConcurrentDictionary<Guid, ResponseContext> responseContexts =
          new ConcurrentDictionary<Guid, ResponseContext>();

        public ResponseContext CreateReponseContext(Guid id, int timeoutMS)
        {
            var context = new ResponseContext(id, timeoutMS);
            if (!this.responseContexts.TryAdd(id, context))
            {
                throw new Exception("Could create response context");
            }
            return context;
        }

        public void RemoveResponseContext(ResponseContext context)
        {
            this.responseContexts.TryRemove(context.Id, out ResponseContext _);
        }

        public void SetResponse(Guid id, Message message)
        {
            this.responseContexts.TryRemove(id, out ResponseContext context);
            if (context != null)
            {
                context.SetResponse(message);
            }
        }
    }

    class ResponseContext
    {
        public readonly Guid Id;

        bool _isSuccess;
        int _timeout;
        Message _response;
        AutoResetEvent _evt = new AutoResetEvent(false);

        public ResponseContext(Guid id, int timeout)
        {
            this.Id = id;
            _timeout = timeout;
        }

        public void SetResponse(Message response)
        {
            _response = response;
            _isSuccess = true;
            _evt.Set();
        }

        public Message GetResponse()
        {
            _evt.WaitOne(_timeout);
            if (!_isSuccess)
            {
                throw new Exception("Timeout reached without reponse");
            }
            return _response;
        }
    }
}
