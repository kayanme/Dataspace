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

        internal sealed class HeavyRebalancer : Rebalancer
        {

            private struct RangeDesc
            {
                public float Range;
                public int Root;
            }

            private readonly RangeDesc[,] _ranges;

            private readonly CacheNode<TKey, TValue>[] _nodes;

            private readonly int _activePath;

            private readonly int _outPath;

            private CacheNode<TKey, TValue> _oldRoot;

            public override int OutPath
            {
                get { return _outPath; }
            }

            private readonly int _actNodes;

            private bool _calculated;

            private void RecursiveFill(CacheNode<TKey, TValue> node, ref int curPosition, ref float sum)
            {
                var lnode = node.GetLeftNode(_activePath);
                if (lnode != null)
                    RecursiveFill(lnode, ref curPosition, ref sum);
                _nodes[curPosition] = node;
                sum += _ranges[curPosition, curPosition].Range = node.GetProbability();
                _ranges[curPosition, curPosition].Root = curPosition;
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
                for (int i = 0; i < actNodes; i++)//приводим усредненные частоты к вероятностям
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


            public HeavyRebalancer(CacheNode<TKey, TValue> root, int nodeCount, int activePath, int outPath)
            {
                _ranges = new RangeDesc[nodeCount, nodeCount];
                _nodes = new CacheNode<TKey, TValue>[nodeCount];
                _activePath = activePath;
                _outPath = outPath;
                _actNodes = FillAndReturnNodesCount(root);
                _oldRoot = root;
            }

            public override float Rebalance(CancellationToken cancelling = default(CancellationToken))
            {
                for (int diff = 1; diff < _actNodes; diff++)
                {
                    Parallel.For(0, _actNodes - diff,
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

                _calculated = true;
                return _ranges[0, _actNodes - 1].Range;
            }


            public override CacheNode<TKey, TValue> ConstructNewTreeAfterCalculation()
            {
                int depth = 0;
                var range = _ranges[0, _actNodes - 1];
                var node = _nodes[range.Root];
                node._depth[_outPath] = 0;
                if (Settings.CheckCycles)
                {
                    if (!_oldRoot.CheckTree(_activePath))
                        Debugger.Break();
                    if (!_oldRoot.CheckTree(_outPath))
                        Debugger.Break();
                    if (!node.CheckTree(_activePath))
                        Debugger.Break();
                    if (!node.CheckTree(_outPath))
                        Debugger.Break();
                }
                foreach (var cnode in _nodes.Where(k => k != null))
                {
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
                ConstructNewTreeAfterCalculation(0, _actNodes - 1, ref depth);

                if (Settings.CheckCycles)
                {
                    if (!_oldRoot.CheckTree(_activePath))
                        Debugger.Break();
                    if (!_oldRoot.CheckTree(_outPath))
                        Debugger.Break();
                    if (!node.CheckTree(_activePath))
                        Debugger.Break();
                    if (!node.CheckTree(_outPath))
                        Debugger.Break();
                }
                return _nodes[_ranges[0, _actNodes - 1].Root];
            }

            public override void Dispose()
            {
                _oldRoot = null;
                for (int i = 0; i < _nodes.Length; i++)
                    _nodes[i] = null;
            }
        }     
    }
}
