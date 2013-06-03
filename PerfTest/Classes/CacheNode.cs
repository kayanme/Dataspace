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

namespace PerfTest.Classes
{
    internal class CacheNode
    {
        private CompositionContainer _container;        

        public string Name { get; private set; }

        public static Assembly ResourceAssembly;

        private NodeUplink _uplink;

        private IResourceGetterFactory _getter;

        private IResourcePosterFactory _poster;

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

        public void ConnectToDownNode(CacheNode node)
        {
            var uplink = node.GetUpLink();
            var downlink = new NodeDownlink();
            uplink.Subscribers.Add(downlink);
            _container.ComposeExportedValue(downlink);
            _getter = new Downgetter(node._container);
            _poster = new Downposter(node._container);
        }

        public void Start()
        {            
            _container.ComposeParts(_getter,_poster);
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
