using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Statistics.Events
{
    public class BranchChangedEvent:StatisticEvent
    {
        public TimeSpan Length { get; private set; }
        public string ResourceType { get; private set; }

        public BranchChangedEvent(string name, DateTime time,TimeSpan length,string resType) : base(name, time)
        {
            ResourceType = resType;
            Length = length;
        }
    }
}
