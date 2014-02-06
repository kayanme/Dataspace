using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.Attributes.CachingPolicies;

namespace Dataspace.Common.ServiceResources
{
    
    public sealed class Registration
    {
        public Guid ResourceKey { get; private set; }
        public string ResourceName { get; private set; }
        public Type ResourceType { get; private set; }
        public bool IsSecuritized { get; private set; }
        public bool IsCacheData { get; private set; }
        public bool CollectRareItems { get; private set; }
        public IEnumerable<CachingPolicyAttribute> CachingPolicy { get; private set; }



        public Registration(string name,Type type,bool securitized,bool cacheData,bool collectRareItems,IEnumerable<CachingPolicyAttribute> cachingPolicies)
        {
            ResourceKey = Guid.NewGuid();
            ResourceName = name;
            ResourceType = type;
            IsSecuritized = securitized;
            IsCacheData = cacheData;
            CollectRareItems = collectRareItems;
            CachingPolicy = cachingPolicies;
        }
    }
}

