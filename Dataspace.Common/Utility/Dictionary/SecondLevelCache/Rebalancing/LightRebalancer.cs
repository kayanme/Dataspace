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
            private readonly LightRebalanceMethod _method = new LightRebalanceMethod();
            private int _nodeCount;
            public LightRebalancer(CacheNode<TKey, TValue> root, int nodeCount, int activePath, int outPath)
            {
                _nodeCount = nodeCount;
                _activePath = activePath;
                _outPath = outPath;              
                _oldRoot = root;
            }
        
            public override int OutPath
            {
                get { return _outPath; }
            }

            public override CacheNode<TKey, TValue> ConstructNewTreeAfterCalculation()
            {
                var root = _method.ReturnRoot();
                _method.BuildTree(root,OutPath);
                return root;
            }

            public override float Rebalance(CancellationToken cancelling = new CancellationToken())
            {
                var nodes = new CacheNode<TKey, TValue>[_nodeCount];
                int pos = 0;
                RecursiveFill(_oldRoot,ref pos,nodes,_activePath);
                FillNodesArray(_oldRoot, nodes, _activePath);
                return 1;
            }

            public override void Dispose()
            {
                _method.Dispose();            
            }
        }
    }
}
