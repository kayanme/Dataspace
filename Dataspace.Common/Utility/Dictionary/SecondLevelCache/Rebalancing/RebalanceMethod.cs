using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dataspace.Common.Utility.Dictionary
{
    partial class CacheNode<TKey, TValue>
    {
        internal abstract class RebalanceMethod:IDisposable
        {
            public abstract void Load(CacheNode<TKey, TValue>[] nodes);

            public abstract void Rebalance(CancellationToken cancelling = default(CancellationToken));

            public abstract void BuildTree(CacheNode<TKey, TValue> root, int outPath);

            public abstract CacheNode<TKey, TValue> ReturnRoot();

            public abstract void Dispose();
        }
    }
}
