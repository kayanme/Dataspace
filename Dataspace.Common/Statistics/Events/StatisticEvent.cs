using System;

namespace Dataspace.Common.Statistics.Events
{
    public abstract class StatisticEvent
    {
        public DateTime Time { get; private set; }

        protected StatisticEvent(DateTime time)
        {
            Time = time;
        }
    }
}
