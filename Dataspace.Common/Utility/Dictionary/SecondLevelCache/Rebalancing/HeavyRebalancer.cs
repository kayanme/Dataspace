using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dataspace.Common.Data;

namespace Dataspace.Common.Utility.Dictionary
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct RangeDesc
    {
        [FieldOffset(4)]
        public int Root;
        [FieldOffset(0)]
        public float Range;


        [FieldOffset(0)]
        public long L;
        
        public static implicit operator long(RangeDesc rd)
        {
            return rd.L;
        }

        public static implicit operator RangeDesc(long rd)
        {
            return new RangeDesc{L = rd};
        }


    }

    partial class CacheNode<TKey, TValue>
    {

        internal sealed class HeavyRebalancer : Rebalancer
        {

            private HeavyRebalanceMethod _method = new HeavyRebalanceMethod();
         

            private readonly CacheNode<TKey, TValue>[] _nodes;

            private readonly int _activePath;

            private readonly int _outPath;

            private CacheNode<TKey, TValue> _oldRoot;

            public override int OutPath
            {
                get { return _outPath; }
            }


                


            public HeavyRebalancer(CacheNode<TKey, TValue> root, int nodeCount, int activePath, int outPath)
            {            
                _nodes = new CacheNode<TKey, TValue>[nodeCount];
                _activePath = activePath;
                _outPath = outPath;
                _oldRoot = root;
                FillNodesArray(root,_nodes,_activePath);
              
            }         

            public override float Rebalance(CancellationToken cancelling = default(CancellationToken))
            {
                _method.Load(_nodes);
                _method.Rebalance(cancelling);
                return 1;
            }


            public override CacheNode<TKey, TValue> ConstructNewTreeAfterCalculation()
            {                                          
                CleanUpTree(_oldRoot,_nodes,_activePath,_outPath);
                var root = _method.ReturnRoot();
                _method.BuildTree(root,OutPath);
                AfterBuildCheck(_oldRoot,_nodes,_activePath,_outPath);
                return root;
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
