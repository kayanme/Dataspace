using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Statistics.Events
{
    public sealed class RebalanceEvent:StatisticEvent 
    {
        public RebalanceEvent(string name,DateTime time,string resourceType,string stage,TimeSpan length = default(TimeSpan)) : base(name,time)
        {
            Stage = stage;
            ResourceType = resourceType;
            Length = length;
        }

        public string Stage { get; set; }
        public string ResourceType { get; private set; }
        public TimeSpan Length { get; private set; } 
    }
}
