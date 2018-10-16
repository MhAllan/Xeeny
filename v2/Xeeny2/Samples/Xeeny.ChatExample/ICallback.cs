using Xeeny.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.ChatExample
{
    public interface ICallback
    {
        //Must return void or Task, in both cases the server doesn't block
        void OnServerUpdates(string msg);

        void WhoIsOnline(string[] online);
    }
}
