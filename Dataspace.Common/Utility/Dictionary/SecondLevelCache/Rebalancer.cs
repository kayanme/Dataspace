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
        }

    }
}
