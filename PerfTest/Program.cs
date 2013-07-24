using System;
using System.Reflection;
using System.Threading.Tasks;
using Indusoft.Test.MockresourceProviders;
using PerfTest.Classes;
using PerfTest.Commands;
using PerfTest.Metadata;

namespace PerfTest
{
    class Program
    {
        private static CacheNode _upnode1;

        private static CacheNode _upnode2;

        private static CacheNode _upnode3;

        private static CacheNode _upnode4;

        private static CacheNode _centnode;       

        private static void Initialize()
        {
            var init = new ResourceTestSystemInitiation();
            var res = init.CreateResources(ResourceSpaceDescriptions.Count);
            ResourceSpaceDescriptions.ResourceAssembly = res;
            AppDomain.CurrentDomain.AssemblyResolve +=
                (e, a) =>
                    {
                        if (a.Name.Contains("TestResources"))
                            return res;
                        throw new Exception();
                    };
            var store = new Store();
           
            _upnode1 = new CacheNode("Client1",res,store);
            _upnode2 = new CacheNode("Client2",res,store);
            _upnode3 = new CacheNode("Client3", res, store);
            _upnode4 = new CacheNode("Client4", res, store);
            _centnode = new CacheNode("Central", res, store);
            _upnode1.ConnectToDownNode(_centnode,TimeSpan.FromMilliseconds(30));
            _upnode2.ConnectToDownNode(_centnode, TimeSpan.FromMilliseconds(30));
            _upnode3.ConnectToDownNode(_centnode, TimeSpan.FromMilliseconds(30));
            _upnode4.ConnectToDownNode(_centnode, TimeSpan.FromMilliseconds(30));
            _upnode1.Start();
            _upnode2.Start();
            _upnode3.Start();
            _upnode4.Start();
            _centnode.Start();
        }

        private static void Administration(CacheNode node,int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                node.PostResourceFromScratch();
                node.PostResourceFromScratch("Client1");
                node.PostResourceFromScratch("Client2");
                node.PostResourceFromScratch("Client3");
                node.PostResourceFromScratch("Client4");       
                node.QueryResourceByOtherResource();
                node.GetQueriedResource();
                node.PostResourceFromScratch();
            }
            node.ClearMemory(0);
        }

        private static void Work(CacheNode node,string rootResName)
        {
            node.QueryResourceByNode(rootResName);
            node.GetAllQueriedResource();          
            node.GetResource();
            node.PostResourceFromMemory();
            node.QueryResourceByNodeAndOtherResource();
            node.GetAllQueriedResource();
            for (int i = 0; i < 20;i++ )
                node.PostResourceFromMemory(node.Name);
            node.QueryResourceByOtherResource();
            node.QueryResourceByOtherResource();
            node.QueryResourceByOtherResource();
            node.GetAllQueriedResource();
            node.PostResourceFromScratch();
            node.GetResource();
            node.ClearMemory(300);
        }

        private static void MaintanceWork(CacheNode node)
        {
            node.QueryAllOfSomeResource();
            node.GetAllQueriedResource();          
            node.GetResource();
            node.PostResourceFromMemory();
            node.QueryResourceByOtherResource();
            node.GetAllQueriedResource(); 
            node.PostResourceFromMemory();
            node.PostResourceFromMemory();
            node.PostResourceFromMemory();
            node.PostResourceFromMemory();
            node.PostResourceFromScratch();
            node.GetResource();
            node.QueryResourceByOtherResource();              
            node.GetAllQueriedResource(); 
            node.PostResourceFromMemory();
            node.PostResourceFromMemory();
            node.ClearMemory(500);
        }

        static void Main(string[] args)
        {            
            Initialize();
            Console.WriteLine("Initialization complete");
            Parallel.Invoke(new ParallelOptions {TaskScheduler = Task.Factory.Scheduler},
                            () => Administration(_upnode1,200),
                            () => Administration(_upnode2,200),
                            () => Administration(_upnode3, 200),
                            () => Administration(_upnode4, 200));
            Console.WriteLine("Administration complete");
            for (var i = 0; i < 500; i++)
            {
                Parallel.Invoke(new ParallelOptions{TaskScheduler = Task.Factory.Scheduler},
                    () => Work(_upnode1,"Res0"),
                    () => Work(_upnode2,"Res1"),
                    () => Work(_upnode1, "Res2"),
                    () => Work(_upnode2, "Res3"),
                    ()=>  MaintanceWork(_centnode)
                    );
               
               
               if (i % 100 == 0) Console.WriteLine(i);
               if (i % 10 == 0)
               {
                   Console.WriteLine("Client1 - {0}",_upnode1.ResInMemory);
                   Console.WriteLine("Client2 - {0}", _upnode2.ResInMemory);
                   Console.WriteLine("Client3 - {0}", _upnode3.ResInMemory);
                   Console.WriteLine("Client4 - {0}", _upnode4.ResInMemory);
                   Console.WriteLine("Service - {0}", _centnode.ResInMemory);
               }
            }
            Console.ReadLine();
        }
    }
}
