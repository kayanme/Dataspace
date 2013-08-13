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
        internal abstract class Rebalancer:IDisposable
        {
            public abstract int OutPath { get; }
            public abstract CacheNode<TKey, TValue> ConstructNewTreeAfterCalculation();

            public abstract float Rebalance(CancellationToken cancelling = default(CancellationToken));
            public abstract void Dispose();


            protected void CleanUpTree(CacheNode<TKey,TValue> _oldRoot,IEnumerable<CacheNode<TKey,TValue>> _nodes,int _activePath,int _outPath)
            {
                if (Settings.CheckCycles)
                {
                    if (!_oldRoot.CheckTree(_activePath))
                        Debugger.Break();
                    if (!_oldRoot.CheckTree(_outPath))
                        Debugger.Break();
                }
                foreach (var cnode in _nodes.Where(k => k != null))
                {
                    var lnode = cnode.GetLeftNode(_outPath);
                    if (lnode != null)
                        cnode.UnfixChild(lnode, _outPath);
                    var rnode = cnode.GetRightNode(_outPath);
                    if (rnode != null)
                        cnode.UnfixChild(rnode, _outPath);
                    cnode._depth[_activePath] = 0;
                    cnode.SetLeftNode(null, _outPath);
                    cnode.SetRightNode(null, _outPath);
                }
            }

            protected void AfterBuildCheck(CacheNode<TKey, TValue> _oldRoot, IEnumerable<CacheNode<TKey, TValue>> _nodes, int _activePath, int _outPath)
            {
                if (Settings.CheckCycles)
                {
                    foreach (var cnode in _nodes.Where(k => k != null))
                        if (cnode.HasLeftNode(_outPath) || cnode.HasRightNode(_outPath))
                            Debugger.Break();
              
                    if (!_oldRoot.CheckTree(_activePath))
                        Debugger.Break();
                    if (!_oldRoot.CheckTree(_outPath))
                        Debugger.Break();
                }
            }

            protected void RecursiveFill(CacheNode<TKey, TValue> node, ref int curPosition, CacheNode<TKey, TValue>[] nodes,int _activePath)
            {
                var lnode = node.GetLeftNode(_activePath);
                if (lnode != null)
                    RecursiveFill(lnode, ref curPosition, nodes, _activePath);
                nodes[curPosition] = node;
                curPosition++;
                var rnode = node.GetRightNode(_activePath);
                if (rnode != null)
                    RecursiveFill(rnode, ref curPosition, nodes, _activePath);
            }

            protected void FillNodesArray(CacheNode<TKey, TValue> root, CacheNode<TKey, TValue>[] nodes, int activePath)
            {
                int actNodes = 0;
                RecursiveFill(root, ref actNodes, nodes,activePath);
            }     
        }

    }
}
