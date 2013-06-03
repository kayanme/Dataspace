using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Statistics;
using Dataspace.Common.Statistics.Events;
using Dataspace.Loggers.Mappers;

namespace Dataspace.Loggers
{
    [Export(typeof(IStatisticsSubscriber))]
    public sealed class SQLLogger:IStatisticsSubscriber
    {
        public void OnNext(IEnumerable<StatisticEvent> value)
        {
            using(var context = new LogContext())
            {
                foreach (var statisticEvent in value)
                {
                    
                    context.Set<StatisticEvent>().Add(statisticEvent);
                }
                context.SaveChanges();
            }
        }

        public void OnError(Exception error)
        {
         
        }

        public void OnCompleted()
        {
         
        }
    }
}
