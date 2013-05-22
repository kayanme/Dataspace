using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Statistics.Events
{
    public class UnactualGetEvent : StatisticEvent
    {
        public Guid Key { get; private set; }
        public string ResourceType { get; private set; }


        public UnactualGetEvent(Guid key, string type, DateTime time)
            : base(time)
        {
            Key = key;
            ResourceType = type;           
        }
    }
}
