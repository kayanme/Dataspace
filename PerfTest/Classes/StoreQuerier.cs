using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Indusoft.Test.MockresourceProviders;
using PerfTest.Metadata;

namespace PerfTest.Classes
{
    [Export(typeof(IResourceQuerierFactory))]
    internal sealed class StoreQuerier:IResourceQuerierFactory
    {
        [Import] 
        private Store _store;

        [Import]
        private IGenericPool _pool;

        public FormedQuery CreateQuerier(string type, string nmspc, string[] args)
        {
            var store = _store[ResourceSpaceDescriptions.ResourceAssembly.GetType(type)];
            if (!args.Any())
                return vals => { lock (store) return store.Select(k => k.Key).ToArray(); };

            if (args.Count() == 1)
            {
                if (args[0] == "Affinity1")
                {
                    return vals =>
                               {
                                   lock (store)
                                       return
                                           store.Where(
                                               k => (k.Value as ResBase).ResourceAffinity1 == new Guid((string) vals[0]))
                                               .Select(k => k.Key).ToArray();
                               };
                }
                if (args[0] == "Affinity2")
                {
                    return vals =>
                               {
                                   lock (store)
                                       return
                                           store.Where(
                                               k => (k.Value as ResBase).ResourceAffinity2 == new Guid((string) vals[0]))
                                               .Select(k => k.Key).ToArray();
                               };
                }
                if (args[0] == "Node")
                {
                    return vals =>
                               {
                                   lock (store)
                                       return store.Where(k => (k.Value as ResBase).NodeAffinity == (string) vals[0])
                                           .Select(k => k.Key).ToArray();
                               };
                }
            }
            if (args.Count() == 2)
            {
                if (args[0] == "Affinity1")
                {
                    return vals =>
                    {
                        lock (store)
                            return
                                store.Where(k => (k.Value as ResBase).ResourceAffinity2 == new Guid((string)vals[0]))
                                    .Where(k => (k.Value as ResBase).NodeAffinity == (string)vals[1])
                                    .Select(k => k.Key).ToArray();
                    };
                }
                if (args[0] == "Affinity2")
                {
                    return vals =>
                    {
                        lock (store)
                            return
                                store.Where(k => (k.Value as ResBase).ResourceAffinity2 == new Guid((string)vals[0]))
                                     .Where(k => (k.Value as ResBase).NodeAffinity == (string)vals[1])
                                     .Select(k => k.Key).ToArray();
                    };
                }
            }
            throw new InvalidOperationException();
        }
    }
}
