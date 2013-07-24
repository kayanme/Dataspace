using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Dataspace.Common.Data;

namespace Dataspace.Common.Utility.Dictionary
{
    partial class CacheNode<TKey, TValue>
    {
        internal class LightRebalancer:Rebalancer
        {
            private int _activePath;
            private int _outPath;
            private CacheNode<TKey, TValue> _oldRoot;
            private CacheNode<TKey, TValue>[] _nodes;

            public LightRebalancer(CacheNode<TKey, TValue> root, int nodeCount, int activePath, int outPath)
            {
            
                _nodes = new CacheNode<TKey, TValue>[nodeCount];
                _activePath = activePath;
                _outPath = outPath;              
                _oldRoot = root;
            }

        

            private void RecursiveFill(CacheNode<TKey, TValue> node, ref int curPosition)
            {
                var lnode = node.GetLeftNode(_activePath);
                if (lnode != null)
                    RecursiveFill(lnode, ref curPosition);
                _nodes[curPosition] = node;
               
                curPosition++;
                var rnode = node.GetRightNode(_activePath);
                if (rnode != null)
                    RecursiveFill(rnode, ref curPosition);
            }

            public override int OutPath
            {
                get { return _outPath; }
            }

            public override CacheNode<TKey, TValue> ConstructNewTreeAfterCalculation()
            {
              
                return _nodes[0];
            }

            public override float Rebalance(CancellationToken cancelling = new CancellationToken())
            {
                int pos = 0;
                RecursiveFill(_oldRoot,ref pos);               
                cancelling.ThrowIfCancellationRequested();

                _nodes = _nodes.Where(k=>k!=null).OrderByDescending(k => k!=null? k.GetProbability():0).ToArray();
                var sum = _nodes.Select(k => k.GetProbability()).Sum();
                var root = _nodes[0];
                root._depth[_outPath] = 0;

                if (Settings.CheckCycles)
                {
                    if (!_oldRoot.CheckTree(_activePath))
                        Debugger.Break();
                    if (!_oldRoot.CheckTree(_outPath))
                        Debugger.Break();
                    if (!root.CheckTree(_activePath))
                        Debugger.Break();
                    if (!root.CheckTree(_outPath))
                        Debugger.Break();
                }
                foreach (var cnode in _nodes)
                {
                    cancelling.ThrowIfCancellationRequested();
                    var lnode = cnode.GetLeftNode(_outPath);
                    if (lnode != null)
                        cnode.UnfixChild(lnode,_outPath);
                    var rnode = cnode.GetRightNode(_outPath);
                    if (rnode != null)
                        cnode.UnfixChild(rnode,_outPath);

                    cnode.SetLeftNode(null, _outPath);
                    cnode.SetRightNode(null, _outPath);
                }

                if (Settings.CheckCycles)
                {
                    foreach (var cnode in _nodes.Where(k => k != null))
                        if (cnode.HasLeftNode(_outPath) || cnode.HasRightNode(_outPath))
                            Debugger.Break();
                }
                float expectedRate = 0;
                for (int i = 1; i < _nodes.Length; i++)
                {
                    cancelling.ThrowIfCancellationRequested();
                    int depth = 0;
                    var node = _nodes[i];
                    node._depth[_activePath] = 0;
                    node._leftFix = null;
                    node._rightFix = null;
                    root.AddNode(node, _outPath, ref depth);
                    expectedRate += (node.GetProbability()/sum)*depth;
                }

                if (Settings.CheckCycles)
                {
                    if (!_oldRoot.CheckTree(_activePath))
                        Debugger.Break();
                    if (!_oldRoot.CheckTree(_outPath))
                        Debugger.Break();
                    if (!root.CheckTree(_activePath))
                        Debugger.Break();
                    if (!root.CheckTree(_outPath))
                        Debugger.Break();
                }


                return expectedRate;
            }

            public override void Dispose()
            {
                _nodes = null;
            }
        }
    }
}
