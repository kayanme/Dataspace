using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Statistics.Events
{
    public class ExternalGetEvent:StatisticEvent 
    {
        public Guid Key { get; private set; }
        public string ResourceType { get; private set; }
        public TimeSpan Length { get; private set; }

        public ExternalGetEvent(Guid key, string type, TimeSpan length, DateTime time)
            : base(time)
        {
            Key = key;
            ResourceType = type;
            Length = length;
        }
    }
}
