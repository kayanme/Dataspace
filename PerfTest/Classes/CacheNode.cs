using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Loggers;
using Indusoft.Test.MockresourceProviders;
using Indusoft.Testhelper.Defaults;
using PerfTest.Commands;
using PerfTest.Metadata;

namespace PerfTest.Classes
{
    internal class CacheNode
    {
        private CompositionContainer _container;        

        public string Name { get; private set; }

   

        private NodeUplink _uplink;

        private IResourceGetterFactory _getter;

        private IResourcePosterFactory _poster;

        private IResourceQuerierFactory _querier;

        private Dictionary<ResourceDescription,ResBase> NodeMemory 
            = new Dictionary<ResourceDescription, ResBase>();

        private static Random _rnd = new Random();

        private ResourceDescription SelectResourceFromMemory(bool isNull,string resName = null)
        {
            lock (NodeMemory)
            {             
                var s = (isNull ? NodeMemory.Where(k => k.Value == null) : NodeMemory);
                if (!s.Any())
                    return NodeMemory.First().Key;
                s = resName == null ? s : s.Where(k => k.Key.ResourceName == resName);
                return GetRandom(s).Key;
            }
        }

        private IEnumerable<ResourceDescription> SelectNullResourcesFromMemory()
        {
            lock (NodeMemory)
            {
                return NodeMemory.Where(k => k.Value == null).Select(k=>k.Key).ToArray();
            }
        }     

        public int ResInMemory
        {
            get { return NodeMemory.Count; }
        }

        public static string GetRandomResName()
        {
            return "Res" + _rnd.Next(ResourceSpaceDescriptions.Count);
        }

       
        private static T GetRandom<T>(IEnumerable<T> s)
        {
            var t = _rnd.Next(s.Count() - 1);
            return s.Skip(t).First();
        }

        private Guid? GetRandomResAffinity(string affName)
        {
            lock (NodeMemory)
            {             
                var poss = NodeMemory.Where(k => k.Key.ResourceName == affName).ToArray();
                if (!poss.Any())
                    return null;
                var t = _rnd.Next((int)(poss.Count() * 1.2));
                if (t < poss.Count() - 1)
                    return poss[t].Key.ResourceKey;
                return null;
            }
        }

        public void PostResourceFromMemory(string nodeAffinity = null,string resName=null)
        {
            if (NodeMemory.Keys.Count == 0)
                PostResourceFromScratch();
            var res = SelectResourceFromMemory(false, resName);
            var affName1 = ResourceSpaceDescriptions.Affinities[res.ResourceName].Aff1;
            var affName2 = ResourceSpaceDescriptions.Affinities[res.ResourceName].Aff2;
            Guid? aff1 = GetRandomResAffinity(affName1);
            Guid? aff2 = GetRandomResAffinity(affName2);
            var post = new UntransactedPostCommand(res.ResourceKey, res.ResourceName, aff1, aff2, nodeAffinity);
            ApplyCommand(post);
            lock (NodeMemory)
            NodeMemory[res] = post.Res;
        }

        public void DeleteResource(string resName = null)
        {
            var res = SelectResourceFromMemory(false,resName);
            var del = new UntransactedDeleteCommand(res.ResourceKey, res.ResourceName);
            ApplyCommand(del);
            lock (NodeMemory)
                NodeMemory.Remove(res);
        }

        public void PostResourceFromScratch(string nodeAffinity = null, string resName = null)
        {
            var id = Guid.NewGuid();
            var name = resName?? GetRandomResName();
            var affName1 = ResourceSpaceDescriptions.Affinities[name].Aff1;
            var affName2 = ResourceSpaceDescriptions.Affinities[name].Aff2;
            Guid? aff1 = GetRandomResAffinity(affName1);
            Guid? aff2 = GetRandomResAffinity(affName2);

            var post = new UntransactedPostCommand(id, name,aff1,aff2,nodeAffinity);
            ApplyCommand(post);
            lock (NodeMemory)
            NodeMemory.Add(new ResourceDescription { ResourceKey = id, ResourceName = name }, post.Res);
        }

        public void GetResource(string resName = null)
        {            
            var res = SelectResourceFromMemory(false,resName);
            var get = new UntransactedGetCommand(res.ResourceKey, res.ResourceName);
            ApplyCommand(get);
            lock (NodeMemory)
            NodeMemory[res] = get.Res;
        }

        public void GetQueriedResource(string resName = null)
        {
            var res = SelectResourceFromMemory(true,resName);
            var get = new UntransactedGetCommand(res.ResourceKey, res.ResourceName);
            ApplyCommand(get);
            lock (NodeMemory)
                NodeMemory[res] = get.Res;
        }

        public void GetAllQueriedResource(string resName = null)
        {
            var res = SelectNullResourcesFromMemory();
            var get = new UntransactedGetManyCommand(res);
            ApplyCommand(get);
            lock (NodeMemory)
                foreach (var id in get.Res)
                {

                    if (!NodeMemory.ContainsKey(id.Key))
                        NodeMemory.Add(id.Key, id.Value);
                    else
                    {
                        NodeMemory[id.Key] = id.Value;
                    }
                }
        }

        public void QueryResourceByNode(string resName = null)
        {
            var name = resName??GetRandomResName();
            var query = new QueryCommand(Name, name);
            ApplyCommand(query);
            lock (NodeMemory)
                foreach (var id in query.Ids)
                {
                    var key = new ResourceDescription
                                  {
                                      ResourceKey = id,
                                      ResourceName = query.TargetName
                                  };
                    if (!NodeMemory.ContainsKey(key))
                        NodeMemory.Add(key, null);                   
                }
        }

        public void QueryResourceByNodeAndOtherResource(string resName = null)
        {
            var res = SelectResourceFromMemory(false, resName);
            var affNum = _rnd.Next(2) + 1;
            var query = new QueryCommand(res.ResourceKey, affNum,res.ResourceName,Name);
            ApplyCommand(query);
            lock (NodeMemory)
                foreach (var id in query.Ids)
                {
                    var key = new ResourceDescription
                    {
                        ResourceKey = id,
                        ResourceName = query.TargetName
                    };
                    if (!NodeMemory.ContainsKey(key))
                        NodeMemory.Add(key, null);
                }
        }

        public void QueryAllOfSomeResource(string resName = null)
        {
            var name = resName??GetRandomResName();
            var query = new QueryCommand(name);
            ApplyCommand(query);
            lock (NodeMemory)
                foreach (var id in query.Ids)
                {
                    var key = new ResourceDescription
                    {
                        ResourceKey = id,
                        ResourceName = query.TargetName
                    };
                    if (!NodeMemory.ContainsKey(key))
                        NodeMemory.Add(key, null);
                }
        }

        public void QueryResourceByOtherResource(string resName = null)
        {
            var res = SelectResourceFromMemory(false, resName);
            var affNum = _rnd.Next(2) + 1;
            var query = new QueryCommand(res.ResourceKey, affNum, res.ResourceName);
            ApplyCommand(query);
            lock (NodeMemory)
                foreach (var id in query.Ids)
                {
                    var key = new ResourceDescription
                    {
                        ResourceKey = id,
                        ResourceName = query.TargetName
                    };
                    if (!NodeMemory.ContainsKey(key))
                        NodeMemory.Add(key, null); 
                }
        }

        public void ClearMemory(int threshold)
        {
            lock (NodeMemory)
            {
                if (NodeMemory.Count <= threshold)
                    return;
                var c = _rnd.Next(NodeMemory.Count - threshold);
                for (int i =0;i<c;i++)
                {
                    var k = GetRandom(NodeMemory.Keys);
                    NodeMemory.Remove(k);
                }
            }
        }

        private CompositionContainer CreateContainerNode(string name, Assembly ass,Store store)
        {
            Name = name;
            var catalog = new AggregateCatalog(
                new AssemblyCatalog(typeof (ITypedPool).Assembly),
                new TypeCatalog(typeof(DefaultSecProvider),typeof(SecContextFactor)),
                new AssemblyCatalog(ass),
                new TypeCatalog(typeof (SQLLogger))
                );
            var container = new CompositionContainer(catalog);
            container.ComposeExportedValue(store);
            _getter = new Factory();
            _poster = new Writer();
            _querier = new StoreQuerier();
            return container;
        }

        public CacheNode(string name, Assembly res,Store store)
        {
            _container = CreateContainerNode(name, res,store);
        }

        private NodeUplink GetUpLink()
        {
            if (_uplink !=null)
                return _uplink;          
            _uplink =  new NodeUplink();
            _container.ComposeExportedValue(_uplink);
            return _uplink;
        }

        public void ConnectToDownNode(CacheNode node,TimeSpan delay)
        {
            var uplink = node.GetUpLink();
            var downlink = new NodeDownlink();
            uplink.Subscribers.Add(downlink);
            _container.ComposeExportedValue(downlink);
            _getter = new Downgetter(node._container,delay);
            _poster = new Downposter(node._container,delay);
            _querier = new LinkQuery(node._container, delay);
        }

        public void Start()
        {            
            _container.ComposeParts(_getter,_poster,_querier);
            _container.GetExportedValue<ICacheServicing>()
                      .Initialize(new Settings{InstanceName = Name}, _container);
        }


        public bool ApplyCommand<T>(NodeCommandBase<T> command)
        {
            var service = _container.GetExportedValue<T>();
            command.Do(service);
            return command.Check(_container.GetExportedValue<Store>());
        }
    }
}
