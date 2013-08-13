using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dataspace.Common.Utility.Dictionary
{
    partial class CacheNode<TKey, TValue>
    {
        private class HeavyRebalanceMethod:RebalanceMethod 
        {
            private long[,] _ranges;

            private int _actNodes;

            private unsafe long* _uRanges;

            private CacheNode<TKey, TValue>[] _nodes;

            public override void Load(CacheNode<TKey, TValue>[] nodes)
            {
                _nodes = nodes;
                _actNodes = nodes.Length;
                _ranges = new long[nodes.Length, nodes.Length];
                float sum = 0;
                for (int i = 0; i < nodes.Length; i++)//приводим усредненные частоты к вероятностям
                {
                    var node = nodes[i];
                    var prob = node.GetProbability();
                    sum += prob;
                    _ranges[i, i] =
                        new RangeDesc
                        {
                            Range = prob,
                            Root = i
                        };
                  
                }
                 for (int i = 0; i < nodes.Length; i++)//приводим усредненные частоты к вероятностям
                 {
                     var r = (RangeDesc) _ranges[i, i];
                     r.Range /= sum;
                     _ranges[i, i] = r;
                 }
            }

            private unsafe void RebalanceStep(int start, int diff)
            {
                var ranges = _uRanges;
                int end = start + diff;// *(float*)& - вытаскивает поле Range из записи
                float levelShiftSum = *(float*)&ranges[start * _actNodes + start]
                                      + *(float*)&ranges[end * _actNodes + end];
                float minRange;
                int minRoot;

                var onlyRightBestTime = *(float*)&ranges[(start + 1) * _actNodes + end];
                var onlyLeftBestTime = *(float*)&ranges[start * _actNodes + end - 1];

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
                /* нижеследующий код является оптимизированной в плане вычислений версией данного
                for (var possRoot = start + 1; possRoot < end; possRoot++)
                {
                    var leftSubTreeBestTime =
                        *(float*)&ranges[start * _actNodes + possRoot - 1];
                         //от start*_actNodes+start, до start*_actNodes+end-1, шаг +1
                    var rightSubTreeBestTime =
                        *(float*)&ranges[(possRoot + 1) * _actNodes + end];
                         //от (start+2)*_actNodes+end, до (end+1)*_actNodes+end,шаг +_actNodes
                    var newPossBestTime = leftSubTreeBestTime + rightSubTreeBestTime;
           
                    if (minRange > newPossBestTime)
                    {
                        minRange = newPossBestTime;
                        minRoot = possRoot;
                    }
                    levelShiftSum +=
                        *(float*)&ranges[possRoot * _actNodes + possRoot];
                          //от (start+1)*_actNodes+start+1, до end*_actNodes+end, шаг +_actNodes+1
                }*/

               int leftSubTreeShift = start*_actNodes + start;
               int rightSubTreeShift = (start + 2) * _actNodes + end;
               int resultShift = (start + 1)*_actNodes + start + 1;
               for (var possRoot = start + 1; possRoot < end; possRoot++)
               {

                   var leftSubTreeBestTime = *(float*)&ranges[leftSubTreeShift];                     
                   var rightSubTreeBestTime = *(float*)&ranges[rightSubTreeShift];                    
                   var newPossBestTime = leftSubTreeBestTime + rightSubTreeBestTime;           
                   if (minRange > newPossBestTime)
                   {
                       minRange = newPossBestTime;
                       minRoot = possRoot;
                   }
                   levelShiftSum +=  *(float*)&ranges[resultShift];
                   leftSubTreeShift += 1;
                   rightSubTreeShift += _actNodes;
                   resultShift += (_actNodes + 1);
               }

                ranges[start * _actNodes + end] = new RangeDesc
                {
                    Range = levelShiftSum + minRange,
                    Root = minRoot
                };
            }

            public unsafe override void Rebalance(CancellationToken cancelling = default(CancellationToken))
            {
                fixed (long* ranges = _ranges)
                {
                    _uRanges = ranges;
                    for (int diff = 1; diff < _actNodes; diff++)
                    {
                        Parallel.For(0, _actNodes - diff,
                                     (start) => RebalanceStep(start, diff));
                        if (cancelling != default(CancellationToken))
                            cancelling.ThrowIfCancellationRequested();
                    }
                }
             
             
            }

            private void ConstructNewTreeAfterCalculation(int start, int end, ref int depth,int outPath)
            {
                
                depth++;
                var range = (RangeDesc)_ranges[start, end];
                var node = _nodes[range.Root];
                int depth2 = 0;           
                node._leftFix = null;
                node._rightFix = null;
                depth2 = 0;
                if (range.Root - 1 >= start)
                {
                    var leftRoot = _nodes[((RangeDesc)_ranges[start, range.Root - 1]).Root];
                    node.AddNode(leftRoot, outPath, ref depth2);
                    ConstructNewTreeAfterCalculation(start, range.Root - 1, ref depth,outPath);
                }
                if (range.Root + 1 <= end)
                {
                    var rightRoot = _nodes[((RangeDesc)_ranges[range.Root + 1, end]).Root];
                    node.AddNode(rightRoot, outPath, ref depth2);
                    ConstructNewTreeAfterCalculation(range.Root + 1, end, ref depth,outPath);
                }


            }

            public override void BuildTree(CacheNode<TKey, TValue> root,int outPath)
            {
                int depth = 0;
                ConstructNewTreeAfterCalculation(0, _actNodes - 1, ref depth,outPath);
            }

            public override CacheNode<TKey, TValue> ReturnRoot()
            {
                return _nodes[((RangeDesc) _ranges[0, _actNodes - 1]).Root];
            }

            public unsafe override void Dispose()
            {
                _uRanges = null;
                
            }
        }
    }
}
