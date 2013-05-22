using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Statistics.Events
{
    public class ExternalSerialGetEvent : StatisticEvent
    {
        public IEnumerable<Guid> Keys { get; private set; }
        public string ResourceType { get; private set; }
        public TimeSpan Length { get; private set; }


        public ExternalSerialGetEvent(IEnumerable<Guid> keys, string type, TimeSpan length, DateTime time)
            : base(time)
        {
            Keys = keys;
            ResourceType = type;
            Length = length;
        }
    }
}
