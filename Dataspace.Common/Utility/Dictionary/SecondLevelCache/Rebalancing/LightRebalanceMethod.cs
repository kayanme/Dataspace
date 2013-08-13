using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dataspace.Common.Utility.Dictionary
{
    partial class CacheNode<TKey, TValue>
    {
        private class LightRebalanceMethod : RebalanceMethod
        {
            private CacheNode<TKey, TValue>[] _nodes;

            public override void Load(CacheNode<TKey, TValue>[] nodes)
            {
                _nodes = nodes.Where(k => k != null).OrderByDescending(k => k != null ? k.GetProbability() : 0).ToArray();
                var sum = nodes.Select(k => k.GetProbability()).Sum();
            }

            public override void Rebalance(CancellationToken cancelling = new CancellationToken())
            {             
                cancelling.ThrowIfCancellationRequested();
            }

            public override void BuildTree(CacheNode<TKey, TValue> root, int outPath)
            {
                for (int i = 1; i < _nodes.Length; i++)
                {               
                    int depth = 0;
                    var node = _nodes[i];                
                    node._leftFix = null;
                    node._rightFix = null;
                    root.AddNode(node, outPath, ref depth);              
                }
                
            }

            public override CacheNode<TKey, TValue> ReturnRoot()
            {
                return  _nodes[0];
            }

            public override void Dispose()
            {
                throw new NotImplementedException();
            }
        }
    }
}
