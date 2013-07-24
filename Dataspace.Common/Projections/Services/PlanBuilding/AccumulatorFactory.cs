using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Dataspace.Common.Projections.Classes;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.Utility;
using Provider = Dataspace.Common.Utility.Accumulator<Dataspace.Common.Projections.Classes.FrameNode, System.Collections.Generic.IEnumerable<System.Guid>>;
using GroupStorage = System.Collections.Concurrent.ConcurrentDictionary<Dataspace.Common.Projections.Classes.FrameNode, System.Collections.Generic.IEnumerable<System.Guid>>;
using Getter = System.Func<System.Collections.Generic.IEnumerable<System.Guid>,
                         System.Collections.Generic.Dictionary<string, object>,
                         System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Guid,
                                                                                                        System.Collections.Generic.IEnumerable<System.Guid>>>>;


namespace Dataspace.Common.Projections.Services
{
    internal class AccumulatorFactory
    {

        private class FactoryNode
        {
            public ProjectionElement Parent;
            public ProjectionElement Child;
            public ParameterNames Parameters;
            public Provider Provider;
        }

        private readonly IndexedCollection<FactoryNode> _cache
            = new IndexedCollection<FactoryNode>(k => k.Parent,
                                                 k => k.Child,
                                                 k => k.Parameters);

        private Action<FrameNode, IEnumerable<Guid>,DateTime?> CreatePusher(GroupStorage storage)
        {
            return (node, vals,time) => storage.AddOrUpdate(node, vals, (key, old) => vals);
        }

        private Func<FrameNode, IEnumerable<Guid>> CreateGetter(GroupStorage storage)
        {
            return n => storage[n];
        }

        private Predicate<FrameNode> CreateChecker(GroupStorage storage)
        {
            return storage.ContainsKey;
        }

        private class SeqComparer<T> : IEqualityComparer<IEnumerable<T>>
        {
            public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(IEnumerable<T> obj)
            {
                unchecked
                {
                    int sum = 0;
                    foreach (T k in obj)
                        sum += k.GetHashCode();
                    return sum;
                }
            }
        }

        private Provider.SerialGetter CreatePackagerGetter(Getter getter, ParameterNames names)
        {
            return nodes =>
            {
                //группы узлов, у которых одинаковые ограничивающие параметры и которые, как следствие, можно получить пакетным получением
                var nodesGroupedByBounding =
                    nodes.GroupBy(k => names.Select(k2 => k.BoundingParameters[k2]).ToArray(),  new SeqComparer<object>());

                var producedNodes =
                    nodesGroupedByBounding.SelectMany(
                        group =>
                            getter(group.Select(frameNode => frameNode.Key).Distinct(),
                                               names.Zip(group.Key, (n, v) => new { n, v })
                                                    .ToDictionary(p => p.n, p => p.v,StringComparer.InvariantCultureIgnoreCase))
                                 .Select(k=>
                            new
                            {
                                key = k.Key,
                                bounding = group.Key,
                                value = k.Value
                            })).ToArray();

                var childNodes = nodes.ToDictionary(k => k,
                                                    k => producedNodes.Single(k2 => k2.bounding.SequenceEqual(names.Select(k4=>k.BoundingParameters[k4]))
                                                                                 && k2.key == k.Key).value);
                                     
                return childNodes;
            };
        }

        public Provider GetOrCreateAccumulator(FramingPlan plan, Getter getter, ProjectionElement parent, ProjectionElement child, ParameterNames parameters)
        {

           

            var alreadyCreated =
                _cache.FirstOrDefault(k => k.Child == child
                                        && k.Parent == parent
                                        && k.Parameters == parameters);

            if (alreadyCreated != null)
                return alreadyCreated.Provider;

            var storage = new GroupStorage(FrameNode.InPlanComparer);
            plan.AddTempStorage(storage);
            var pusher = CreatePusher(storage);
            var checker = CreateChecker(storage);
            var taker = CreateGetter(storage);
            var packTaker = CreatePackagerGetter(getter, parameters);

            var acc = new Provider(pusher, checker,
                                   taker, packTaker,
                                   FrameNode.InPlanComparer);
            var node = new FactoryNode
            {
                Parent = parent,
                Child = child,
                Parameters = parameters,
                Provider = acc
            };
            _cache.Add(node);
            return acc;
        }
    }
}
