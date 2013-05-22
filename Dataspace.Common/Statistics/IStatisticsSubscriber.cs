using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.Statistics.Events;

namespace Dataspace.Common.Statistics
{
    public interface IStatisticsSubscriber:IObserver<IEnumerable<StatisticEvent>>
    {
    }
}
