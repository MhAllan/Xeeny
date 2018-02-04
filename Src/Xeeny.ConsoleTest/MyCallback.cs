using Xeeny.Attributes;
using Xeeny.Dispatching;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.ConsoleTest
{
    class MyCallback : ICallback
    {
        public Task OnCallBack(string serverMessage)
        {
            Console.WriteLine(serverMessage);

            return Task.CompletedTask;
        }
    }
}
