using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dataspace.Common.Data;

namespace Dataspace.Common.Utility.Dictionary
{
    partial class CacheNode<TKey, TValue>
    {
        internal class HybridRebalancer : Rebalancer
        {
             private int _activePath;
            private int _outPath;
            private CacheNode<TKey, TValue> _oldRoot;
            private CacheNode<TKey, TValue>[] _nodes;

     
            private readonly int _nodesForHeavyRebalance ;
            private bool _calculated;
            private static TimeSpan _timeFor500NodesToRebalance;

            public int MaxNodesForExactRebalancing = 500;

            private RebalanceMethod _lightRebalance = new LightRebalanceMethod();
            private RebalanceMethod _heavyRebalance = new HeavyRebalanceMethod();

          


            public override int OutPath
            {
                get { return _outPath; }
            }

            public override CacheNode<TKey, TValue> ConstructNewTreeAfterCalculation()
            {
                CleanUpTree(_oldRoot,_nodes,_activePath,_outPath);
                var root = _heavyRebalance.ReturnRoot();
                _heavyRebalance.BuildTree(root,_outPath);
                _lightRebalance.BuildTree(root,_outPath);
                AfterBuildCheck(_oldRoot,_nodes,_activePath,_outPath);
                return root;
            }

            public override float Rebalance(CancellationToken cancelling = default(CancellationToken))
            {
                var w = Stopwatch.StartNew();
                var heavy = new CacheNode<TKey, TValue>[_nodesForHeavyRebalance];
                Array.Copy(_nodes, heavy, _nodesForHeavyRebalance);
                _heavyRebalance.Load(heavy);
                _heavyRebalance.Rebalance(cancelling);
                w.Stop();
                if (_timeFor500NodesToRebalance == default(TimeSpan))
                {
                    _timeFor500NodesToRebalance = w.Elapsed;
                }
                else
                {
                    var r = Math.Pow(MaxNodesForExactRebalancing, 3)/Math.Pow(500, 3);
                    _timeFor500NodesToRebalance = TimeSpan.FromMilliseconds(w.ElapsedMilliseconds/r);
                }
                var light = new CacheNode<TKey, TValue>[_nodes.Count() -  _nodesForHeavyRebalance];

                Array.Copy(_nodes,_nodesForHeavyRebalance, light,0, light.Length);
                _lightRebalance.Load(light);
                _lightRebalance.Rebalance(cancelling);              
                _calculated = true;
                return 1;
            }

            public override void Dispose()
            {
                _oldRoot = null;
                for (int i = 0; i < _nodes.Length; i++)
                    _nodes[i] = null;
            }

            public HybridRebalancer(CacheNode<TKey, TValue> root, int nodeCount, int activePath, int outPath,TimeSpan expectedTimeToEnd)
            {
                 _nodes = new CacheNode<TKey, TValue>[nodeCount];
                _activePath = activePath;
                _outPath = outPath;              
                _oldRoot = root;
                if (_timeFor500NodesToRebalance == default(TimeSpan))
                    MaxNodesForExactRebalancing = 500;
                else
                {
                    var r = expectedTimeToEnd.TotalMilliseconds/_timeFor500NodesToRebalance.TotalMilliseconds;
                    MaxNodesForExactRebalancing = (int)Math.Pow(r*Math.Pow(500, 3), 1/3.0);
                }
                _nodesForHeavyRebalance = Math.Min(nodeCount, MaxNodesForExactRebalancing);


                FillNodesArray(root, _nodes, _activePath);
                Debug.Assert(_nodes.Select(k => k._key).Distinct().Count() == _nodes.Length 
                          && _nodes.Length == nodeCount);
              
            }
        }
    }
}
