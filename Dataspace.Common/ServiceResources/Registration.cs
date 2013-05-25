using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.ServiceResources
{
    
    public sealed class Registration
    {
        public Guid ResourceKey { get; private set; }
        public string ResourceName { get; private set; }
        public Type ResourceType { get; private set; }
        public bool IsSecuritized { get; private set; }
        public bool IsCacheData { get; private set; }



        public Registration(string name,Type type,bool securitized,bool cacheData)
        {
            ResourceKey = Guid.NewGuid();
            ResourceName = name;
            ResourceType = type;
            IsSecuritized = securitized;
            IsCacheData = cacheData;
        }
    }
}

