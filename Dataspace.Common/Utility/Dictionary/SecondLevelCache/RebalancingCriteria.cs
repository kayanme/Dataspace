using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Utility.Dictionary.SecondLevelCache
{
    internal abstract class RebalancingCriteria
    {
        public abstract bool IsRebalanceNeeded<A, B>(SecondLevelCache<A, B> cache);

        public abstract RebalancingMode RecommendedMode<A, B>(SecondLevelCache<A, B> cache);

        public virtual void AfterRebalance()
        {        
        }
    }
}
