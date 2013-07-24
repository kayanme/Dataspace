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

            private struct RangeDesc
            {
                public float Range;
                public int Root;
            }
            private readonly RangeDesc[,] _ranges;
            private readonly int _nodesForHeavyRebalance ;
            private bool _calculated;
            private static TimeSpan _timeFor500NodesToRebalance;

            public int MaxNodesForExactRebalancing = 500;

            private void RecursiveFill(CacheNode<TKey, TValue> node, ref int curPosition, ref float sum)
            {
                var lnode = node.GetLeftNode(_activePath);
                if (lnode != null)
                    RecursiveFill(lnode, ref curPosition, ref sum);
                _nodes[curPosition] = node;               
                curPosition++;
                var rnode = node.GetRightNode(_activePath);
                if (rnode != null)
                    RecursiveFill(rnode, ref curPosition, ref sum);
            }

            private int FillAndReturnNodesCount(CacheNode<TKey, TValue> root)
            {
                int actNodes = 0;
                float sum = 0;
                RecursiveFill(root, ref actNodes, ref sum);
                var d = _nodes.Select(k => k._key).Distinct();
                Debug.Assert(d.Count() == _nodes.Length);          
                Debug.Assert(_nodes.Select(k => k._key).Distinct().Count() == _nodes.Length);
                for (int i = 0; i < _nodesForHeavyRebalance; i++)
                {
                    sum += _ranges[i, i].Range = _nodes[i].GetProbability();
                    _ranges[i, i].Root = i;
                }

                for (int i = 0; i < _nodesForHeavyRebalance; i++)//приводим усредненные частоты к вероятностям
                    _ranges[i, i].Range /= sum;
                return actNodes;
            }

            private void ConstructNewTreeAfterCalculation(int start, int end, ref int depth)
            {
                Debug.Assert(_calculated);
                depth++;
                var range = _ranges[start, end];
                var node = _nodes[range.Root];
                int depth2 = 0;
                node._depth[_activePath] = 0;
                node._leftFix = null;
                node._rightFix = null;
                depth2 = 0;
                if (range.Root - 1 >= start)
                {
                    var leftRoot = _nodes[_ranges[start, range.Root - 1].Root];
                    node.AddNode(leftRoot, _outPath, ref depth2);
                    ConstructNewTreeAfterCalculation(start, range.Root - 1, ref depth);
                }
                if (range.Root + 1 <= end)
                {
                    var rightRoot = _nodes[_ranges[range.Root + 1, end].Root];
                    node.AddNode(rightRoot, _outPath, ref depth2);
                    ConstructNewTreeAfterCalculation(range.Root + 1, end, ref depth);
                }
            }

            public override int OutPath
            {
                get { return _outPath; }
            }

            public override CacheNode<TKey, TValue> ConstructNewTreeAfterCalculation()
            {
                int depth = 0;
                var range = _ranges[0, _nodesForHeavyRebalance - 1];
                var root = _nodes[range.Root];
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
                foreach (var cnode in _nodes.Where(k => k != null))
                {
                    var lnode = cnode.GetLeftNode(_outPath);
                    if (lnode != null)
                        cnode.UnfixChild(lnode, _outPath);
                    var rnode = cnode.GetRightNode(_outPath);
                    if (rnode != null)
                        cnode.UnfixChild(rnode, _outPath);

                    cnode.SetLeftNode(null, _outPath);
                    cnode.SetRightNode(null, _outPath);
                }

                if (Settings.CheckCycles)
                {
                    foreach (var cnode in _nodes.Where(k => k != null))
                        if (cnode.HasLeftNode(_outPath) || cnode.HasRightNode(_outPath))
                            Debugger.Break();
                }
                Debug.Assert(_nodes.Select(k => k._key).Distinct().Count() == _nodes.Length);
                ConstructNewTreeAfterCalculation(0, _nodesForHeavyRebalance - 1, ref depth);
                Debug.Assert(_nodes.Select(k=>k._key).Distinct().Count() ==_nodes.Length);
                if (_nodesForHeavyRebalance < _nodes.Length)
                {
                    
                 
                    foreach (var node in Enumerable.Range(_nodesForHeavyRebalance,_nodes.Length-_nodesForHeavyRebalance)
                        .Select(k=>_nodes[k])
                        .OrderByDescending(k => k != null ? k.GetProbability() : 0))
                    {
                        depth = 0;                       
                
                        node._depth[_activePath] = 0;
                        node._leftFix = null;
                        node._rightFix = null;
                        root.AddNode(node, _outPath, ref depth);
                    }
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



                return root;
            }

            public override float Rebalance(CancellationToken cancelling = default(CancellationToken))
            {
                var w = Stopwatch.StartNew();
                for (int diff = 1; diff < _nodesForHeavyRebalance; diff++)
                {
                    Parallel.For(0, _nodesForHeavyRebalance - diff,
                                 start =>
                                 {
                                     int end = start + diff;
                                     float levelShiftSum = _ranges[start, start].Range + _ranges[end, end].Range;
                                     float minRange;
                                     int minRoot;

                                     var onlyRightBestTime = _ranges[start + 1, end].Range;
                                     var onlyLeftBestTime = _ranges[start, end - 1].Range;

                                     if (onlyRightBestTime > onlyLeftBestTime)
                                     {
                                         minRange = onlyLeftBestTime;
                                         minRoot = end;
                                     }
                                     else
                                     {
                                         minRange = onlyRightBestTime;
                                         minRoot = start;
                                     }

                                     for (var possRoot = start + 1; possRoot < end; possRoot++)
                                     {
                                         var leftSubTreeBestTime = _ranges[start, possRoot - 1].Range;
                                         var rightSubTreeBestTime = _ranges[possRoot + 1, end].Range;
                                         var newPossBestTime = leftSubTreeBestTime + rightSubTreeBestTime;
                                         if (minRange > newPossBestTime)
                                         {
                                             minRange = newPossBestTime;
                                             minRoot = possRoot;
                                         }
                                         levelShiftSum += _ranges[possRoot, possRoot].Range;
                                     }
                                     _ranges[start, end].Range = levelShiftSum + minRange;
                                     _ranges[start, end].Root = minRoot;
                                 });
                    if (cancelling != default(CancellationToken))
                        cancelling.ThrowIfCancellationRequested();
                }
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
                _calculated = true;
                return _ranges[0, _nodesForHeavyRebalance - 1].Range;
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
              
                 _ranges = new RangeDesc[_nodesForHeavyRebalance, _nodesForHeavyRebalance];
             
                FillAndReturnNodesCount(root);
                Debug.Assert(_nodes.Select(k => k._key).Distinct().Count() == _nodes.Length 
                          && _nodes.Length == nodeCount);
              
            }
        }
    }
}
