using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeroconf;

namespace ZeroconfTest.NetFramework
{
    class Program
    {
        static void Main(string[] args)
        {
            var task = ZeroconfResolver.ResolveAsync("_spartancube._tcp.local.");
            task.Wait();
            foreach (var l in task.Result)
            {
                Console.WriteLine(l.DisplayName);
                Console.WriteLine(l.NetworkInterface.Description);
            }
            Console.ReadLine();
        }
    }
}
