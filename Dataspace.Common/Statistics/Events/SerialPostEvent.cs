using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Statistics.Events
{
    public class SerialPostEvent:StatisticEvent
    {
        public TimeSpan Length { get; private set; }

        public SerialPostEvent(string name, DateTime time,TimeSpan length) : base(name, time)
        {
            Length = length;
        }
    }
}
