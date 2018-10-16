using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.ChatExample
{
    class Callback : ICallback
    {
        public string Name { get; set; }

        public void OnServerUpdates(string msg)
        {
            Console.WriteLine($"{Name} received: {msg}");
        }

        public void WhoIsOnline(string[] online)
        {
            var sb = new StringBuilder($"{Name} received: ");
            foreach(var name in online)
            {
                sb.Append($"\n\t{name}");
            }

            Console.WriteLine(sb.ToString());
        }
    }
}
