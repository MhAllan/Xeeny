using Xeeny.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.ConsoleTest
{
    public interface IService
    {
        Task<string> Echo(string message);

        Task<string> CallMeBack();

        [Operation(IsOneWay = true)] //don't wait server execution
        void FireAndForget(string message);
    }

    public interface IOtherService
    {
        [Operation(Name = "AnotherEcho")]
        Task<string> Echo(string message);
    }

    public interface ICallback
    {
        [Operation(IsOneWay = true)]
        Task OnCallBack(string serverMessage);
    }
}
