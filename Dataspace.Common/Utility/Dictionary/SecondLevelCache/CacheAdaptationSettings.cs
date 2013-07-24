using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Utility.Dictionary.SecondLevelCache
{
    public class CacheAdaptationSettings
    {
        public RebalancingMode RebalancingMode { get; internal set; }
        public float GoneIntensityForBranchDecrease { get; internal set; }
        public float GoneIntensityForBranchIncrease { get; internal set; }
        public int CheckThreshold{ get; internal set; }     
        public int MinBranchLengthToRebalance { get; internal set; }
    }
}
