using System;
using System.ComponentModel.Composition;
using System.Linq;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Statistics;
using Dataspace.Common.Utility.Dictionary;
using Common.Utility.Dictionary;

namespace Dataspace.Common.Services
{
  
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class CurrentCachierStorageNoRef : UpgradedCache<Guid, object, NoReferenceUpdatableElement<object>>, ICachierStorage<Guid>
    {
        
        public CurrentCachierStorageNoRef(IStatChannel channel, StatisticsCollector collector) 
            : base(channel,collector)
        {
            
        }
       
    }

  
    [PartCreationPolicy(CreationPolicy.Shared)]   
    internal sealed class CurrentCachierStorageRef : UpgradedCache<Guid, object, UpdatableElement<object>>, ICachierStorage<Guid>
    {

        public CurrentCachierStorageRef( IStatChannel channel, StatisticsCollector collector)
            : base( channel, collector)
        {
            
        }
    }


   
    [PartCreationPolicy(CreationPolicy.Shared)]  
    internal sealed class CurrentCachierStorage : UpgradedCache<string, object, NoReferenceUpdatableElement<object>>, ICachierStorage<string>
    {

        public CurrentCachierStorage( IStatChannel channel, StatisticsCollector collector)
            : base( channel, collector)
        {
            
        }

        void ICachierStorage<string>.SetUpdateNecessity(string key)
        {
            if (key == null)
            {
                Clear();
            }
            else
            {
                var searchKey = key.Substring(0, 16);
                var targetKeys = Keys.Where(k => k.Substring(0, 16) == searchKey);
                foreach (var tKey in targetKeys)
                    SetUpdateNecessity(tKey);
            }
        }
    }
}
