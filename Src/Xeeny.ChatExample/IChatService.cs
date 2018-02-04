using Xeeny.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.ChatExample
{
    public interface IChatService
    {
        //always return Task or Task<T>, this has blocking thread.
        void Join(string id);

        Task Say(string message);

        [Operation(IsOneWay = true)]
        Task TellOthers(string message);

        [Operation(IsOneWay = true)]
        Task PrivateTo(string id, string message);

        Task WhoIsOnline();
    }
}
