using System;
using System.Reflection;
using Indusoft.Test.MockresourceProviders;
using PerfTest.Classes;
using PerfTest.Commands;

namespace PerfTest
{
    class Program
    {
        private static CacheNode _node1;

        private static CacheNode _node2;

        private const int Count = 5;

        private static void Initialize()
        {
            var init = new ResourceTestSystemInitiation();
            var res = init.CreateResources(Count);
            CacheNode.ResourceAssembly = res;
            AppDomain.CurrentDomain.AssemblyResolve +=
                (e, a) =>
                    {
                        if (a.Name.Contains("TestResources"))
                            return res;
                        throw new Exception();
                    };
            var store = new Store();
           
            _node1 = new CacheNode("N1",res,store);
            _node2 = new CacheNode("N2",res,store);
            _node1.ConnectToDownNode(_node2);
            _node1.Start();
            _node2.Start();
        }

        

        static void Main(string[] args)
        {
            
            Initialize();
            var rnd = new Random();

            for (var i = 0; i < 15000; i++)
            {
                var t = rnd.Next(Count);
                var id = Guid.NewGuid();
                var name = "Res" + t;
                var post = new UntransactedPostCommand(id, name);
                _node1.ApplyCommand(post);
                var get = new UntransactedGetCommand(id,name);
                _node1.ApplyCommand(get);
                _node2.ApplyCommand(get);
                _node2.ApplyCommand(post);
                _node1.ApplyCommand(get);
                _node2.ApplyCommand(get);
               if (i % 100 == 0) Console.WriteLine(i);
            }
            Console.ReadLine();
        }
    }
}
