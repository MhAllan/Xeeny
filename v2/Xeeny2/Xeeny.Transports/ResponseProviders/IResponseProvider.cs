using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xeeny.ResponseProviders
{
    public interface IResponseProvider<T>
    {
        ResponseContext<T> CreateResponseContext(Guid id, int timeoutMS);
        ResponseContext<T> CreateResponseContext(Guid id, TimeSpan timeout);
        void RemoveResponseContext(ResponseContext<T> context);
        void SetResponse(Guid id, T response);
        T GetResponse(Guid id);
    }
}
