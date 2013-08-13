using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using Dataspace.Common.Statistics.Events;

namespace Dataspace.Common.Statistics.Subscribers
{
   
    [PartCreationPolicy(CreationPolicy.Shared)]
    public abstract class DebugLogger:IStatisticsSubscriber,IDisposable
    {


        private List<StatisticEvent> _events = new List<StatisticEvent>();

        private Timer _timer = new Timer();

        public abstract string Name { get; }

        protected DebugLogger()
        {
            _timer.AutoReset = true;
            _timer.Elapsed +=
                (o, e) =>
                    {
                        lock (_events)
                        {
                            if (_events.Any())
                              PrintReport(_events);
                            _events.Clear();
                        }
                    };
            _timer.Interval = 60000;

            _timer.Start();
        }

        [Conditional("DEBUG")]
        private void PrintReport(IEnumerable<StatisticEvent> value)
        {
            var posted = value.OfType<PostEvent>();
            var external = value.OfType<ExternalGetEvent>();
            var externalMany = value.OfType<ExternalSerialGetEvent>();
            var postMany = value.OfType<SerialPostEvent>();
            var internl = value.OfType<CachedGetEvent>();
            var unact = value.OfType<UnactualGetEvent>();
            var rebalancing = value.OfType<RebalanceEvent>();
            var branchChange = value.OfType<BranchChangedEvent>();
            string reportPart;
            Debug.Print("Отчет кэша " + Name + ":");

            if (externalMany.Any() || external.Any())
                Debug.Print("   Взяты из базы: ");
            if (external.Any())
            {
                reportPart =
                    string.Join("\n",
                                external.GroupBy(k => k.ResourceType)
                                    .Select(
                                        k =>
                                        new
                                        {
                                            name = k.Key,
                                            count = k.Count(),
                                            time = TimeSpan.FromTicks((long)k.Average(k2 => k2.Length.Ticks)),
                                            total = TimeSpan.FromTicks(k.Sum(k2 => k2.Length.Ticks))
                                        })
                                    .Select(
                                        k => string.Format("   --- {0} {1} раз, всего {3}, в среднем {2}", k.name, k.count, k.time, k.total))
                        );
                Debug.Print(reportPart);
            }
            if (externalMany.Any())
            {
                Debug.Print("     сериями");
                reportPart =
                    string.Join("\n",
                                externalMany.GroupBy(k => k.ResourceType)
                                    .Select(
                                        k =>
                                        new
                                        {
                                            name = k.Key,
                                            count = k.Count(),
                                            time = TimeSpan.FromTicks((long)k.Average(k2 => k2.Length.Ticks)),
                                            total = TimeSpan.FromTicks(k.Sum(k2=>k2.Length.Ticks))
                                        })
                                    .Select(
                                        k => string.Format("   --- {0} {1} раз, всего {3}, в среднем {2}", k.name, k.count, k.time,k.total))
                        );
                Debug.Print(reportPart);
            }
            if (internl.Any())
            {
                Debug.Print("   Взяты из кэша: ");
                reportPart =
                    string.Join("\n",
                                internl.GroupBy(k => k.ResourceType)
                                    .Select(
                                        k =>
                                        new
                                        {
                                            name = k.Key,
                                            count = k.Count(),
                                            time = TimeSpan.FromTicks((long)k.Average(k2 => k2.Length.Ticks)),
                                            total = TimeSpan.FromTicks(k.Sum(k2 => k2.Length.Ticks))
                                        })
                                    .Select(
                                        k => string.Format("   --- {0} {1} раз, всего {3}, в среднем {2}", k.name, k.count, k.time, k.total))
                        );
                Debug.Print(reportPart);
            }
            if (posted.Any())
            {
                Debug.Print("   Записаны: ");
                reportPart =
                    string.Join("\n",
                                posted.GroupBy(k => k.ResourceType)
                                    .Select(
                                        k =>
                                        new
                                        {
                                            name = k.Key,
                                            count = k.Count(),
                                            time = TimeSpan.FromTicks((long)k.Average(k2 => k2.Length.Ticks)),
                                            total = TimeSpan.FromTicks(k.Sum(k2 => k2.Length.Ticks))
                                        })
                                    .Select(
                                        k => string.Format("   --- {0} {1} раз, всего {3}, в среднем {2}", k.name, k.count, k.time, k.total))
                        );
                Debug.Print(reportPart);
            }
            if (postMany.Any())
            {
                Debug.Print("     сериями");
                reportPart = string.Format("   --- {0} раз, всего {2}, в среднем {1}", 
                     postMany.Count(),
                     TimeSpan.FromTicks((long)postMany.Average(k2 => k2.Length.Ticks)),
                     TimeSpan.FromTicks(postMany.Sum(k2 => k2.Length.Ticks)));                       
                Debug.Print(reportPart);
            }
            if (unact.Any())
            {
                Debug.Print("   Стали неактуальны: ");
                reportPart =
                    string.Join("\n",
                                unact.GroupBy(k => k.ResourceType)
                                    .Select(
                                        k =>
                                        new
                                        {
                                            name = k.Key,
                                            count = k.Count()
                                        })
                                    .Select(
                                        k => string.Format("   --- {0} {1} раз", k.name, k.count))
                        );
                Debug.Print(reportPart);
            }

            if (rebalancing.Any())
            {
                Debug.Print("   Была ребалансировка: ");
                reportPart =
                    string.Join("\n",
                                rebalancing.GroupBy(k => k.ResourceType)
                                    .Select(
                                        k =>
                                        new
                                        {
                                            name = k.Key,
                                            time = TimeSpan.FromTicks((long)k.Average(k2 => k2.Length.Ticks)),                                       
                                        })
                                    .Select(
                                        k => string.Format("   --- {0} за среднее время {1}", k.name, k.time))
                        );
                Debug.Print(reportPart);
            }

            if (branchChange.Any())
            {
                Debug.Print("   Изменилась предельная глубина кэша: ");
                reportPart =
                    string.Join("\n",
                                branchChange.GroupBy(k => k.ResourceType)
                                    .Select(
                                        k =>
                                        new
                                        {
                                            name = k.Key,
                                            time = TimeSpan.FromTicks((long)k.Average(k2 => k2.Length.Ticks)),
                                        })
                                    .Select(
                                        k => string.Format("   --- {0} за среднее время {1}", k.name, k.time))
                        );
                Debug.Print(reportPart);
            }
        }

       
        public void OnNext(IEnumerable<StatisticEvent> value)
        {           
            lock(_events)
             _events.AddRange(value);          
        }

        public void OnError(Exception error)
        {
           
        }

        public void OnCompleted()
        {
          
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
