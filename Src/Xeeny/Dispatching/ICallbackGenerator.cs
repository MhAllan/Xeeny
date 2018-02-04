using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Dispatching
{
    public interface ICallbackGenerator : IConnectionProvider
    {
        TCallback GetCallback<TCallback>();
    }
}
