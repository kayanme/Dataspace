using System;

namespace Dataspace.Common.Statistics.Events
{
    public abstract class StatisticEvent
    {
        public DateTime Time { get; private set; }

        public string CacheName { get; private set; }

        public Guid EventKey { get; private set; }

        protected StatisticEvent(string name,DateTime time)
        {
            Time = time;
            CacheName = name;
            EventKey = Guid.NewGuid();
        }
    }
}
