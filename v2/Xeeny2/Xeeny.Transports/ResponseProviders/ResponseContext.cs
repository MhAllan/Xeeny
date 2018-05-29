using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.ResponseProviders
{
    public class ResponseContext<T>
    {
        public readonly Guid Id;

        readonly IResponseProvider<T> _mapper;
        bool _isSuccess;
        int _timeout;
        T _response;
        AutoResetEvent _evt = new AutoResetEvent(false);

        public ResponseContext(IResponseProvider<T> manager, Guid id, int timeoutMS)
        {
            if(timeoutMS <= 0)
            {
                throw new ArgumentException(nameof(timeoutMS));
            }
            Id = id;
            _mapper = manager;
            _timeout = timeoutMS;
        }

        public void SetResponse(T response)
        {
            _response = response;
            _isSuccess = true;
            _evt.Set();
        }

        public T GetResponse()
        {
            _evt.WaitOne(_timeout);
            _mapper.RemoveResponseContext(this);
            if (!_isSuccess)
            {
                throw new Exception("Timeout reached without reponse");
            }
            return _response;
        }
    }
}
