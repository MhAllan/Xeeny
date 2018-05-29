using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.ResponseProviders
{
    public class ResponseProvider<T> : IResponseProvider<T>
    {
        ConcurrentDictionary<Guid, ResponseContext<T>> _responseContexts = new ConcurrentDictionary<Guid, ResponseContext<T>>();

        public ResponseContext<T> CreateResponseContext(Guid id, int timeoutMS)
        {
            var context = new ResponseContext<T>(this, id, timeoutMS);
            if (!this._responseContexts.TryAdd(id, context))
            {
                throw new Exception("Could create response context");
            }
            return context;
        }

        public ResponseContext<T> CreateResponseContext(Guid id, TimeSpan timeSpan)
        {
            return CreateResponseContext(id, (int)timeSpan.TotalMilliseconds);
        }

        public T GetResponse(Guid id)
        {
            _responseContexts.TryRemove(id, out var context);
            if(context == null)
            {
                throw new Exception("No response context was mapped, or it timed out");
            }
            return context.GetResponse();
        }

        public void RemoveResponseContext(ResponseContext<T> context)
        {
            _responseContexts.TryRemove(context.Id, out ResponseContext<T> _);
        }

        public void SetResponse(Guid id, T response)
        {
            _responseContexts.TryRemove(id, out var context);
            if (context != null)
            {
                context.SetResponse(response);
            }
        }
    }
}