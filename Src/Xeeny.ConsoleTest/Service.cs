using Xeeny.Attributes;
using Xeeny.Dispatching;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.ConsoleTest
{
    class Service : IService, IOtherService
    {
        //this is to show InstanceMode and how it works
        string instanceId = Guid.NewGuid().ToString();

        public Task<string> CallMeBack()
        {
            var seconds = 5;
            TimeSpan ts = TimeSpan.FromSeconds(seconds);

            CallBackAfter(ts);


            var msg = $"Server will call back in {seconds} seconds";
            return Task.FromResult(msg);
        }

        async void CallBackAfter(TimeSpan delay)
        {
            var callback = OperationContext.Current.GetCallback<ICallback>();

            await Task.Delay((int)delay.TotalMilliseconds);

            await callback.OnCallBack("This is a server callback");
        }

        public Task<string> Echo(string message)
        {
            var response = $"Response from: {nameof(IService)}, Id {instanceId}, message: {message}";
            return Task.FromResult(response);
        }

        Task<string> IOtherService.Echo(string message)
        {
            var response = $"Response from: {nameof(IOtherService)}, Id {instanceId}, message: {message}";
            return Task.FromResult(response);
        }

        public void FireAndForget(string message)
        {
            Console.WriteLine(message);
        }
    }
}
