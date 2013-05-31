using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Dataspace.Common.Services;
using Dataspace.Common.Statistics;
using Dataspace.Common.Statistics.Events;

namespace Dataspace.Common.Statistics
{

    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class StatisticsCollector
    {

        [Import] 
        private SettingsHolder _settings;

        private Dictionary<string,List<ConcurrentQueue<StatChannel.Mesg>>> _channels 
            = new Dictionary<string, List<ConcurrentQueue<StatChannel.Mesg>>>();

        

        public void RegisterChannel(string type,ConcurrentQueue<StatChannel.Mesg> channel)
        {
            lock(_channels)
            {
                if (!_channels.ContainsKey(type)) 
                    _channels.Add(type,new List<ConcurrentQueue<StatChannel.Mesg>>());
                _channels[type].Add(channel);
            }
        }

        private readonly StatQueue _statQueue;

        private class StatQueue : IObservable<IEnumerable<StatisticEvent>>
        {

            private List<IObserver<IEnumerable<StatisticEvent>>> _subscrs = new List<IObserver<IEnumerable<StatisticEvent>>>();          

            private class MockDisp :IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }

            public IDisposable Subscribe(IObserver<IEnumerable<StatisticEvent>> observer)
            {
                _subscrs.Add(observer);
                return new MockDisp();
            }

            public void PutEvent(IEnumerable<StatisticEvent> evnts)
            {

                foreach (var s in _subscrs)
                {
                    try
                    {
                        s.OnNext(evnts);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            s.OnError(ex);
                        }
                        catch (Exception)//это нормально в данной ситуации. Никто не знает, что в подписчике может случиться, но это сообщение он потеряет. Его проблемы.
                        {
                           
                        }
                       
                    }
                   
                }
            }

           
          
        }

        [ImportingConstructor]
        public StatisticsCollector([ImportMany]IEnumerable<IStatisticsSubscriber> subscribers)
        {
            _statQueue = new StatQueue();
            foreach (var subscriber in subscribers)
              _statQueue.Subscribe(subscriber);
            StartProcessing();
        }

        public void AddAdditionActionToStatisticsQueue(Action action)
        {
            _additionActions.Enqueue(action);
        }

        private ConcurrentQueue<Action> _additionActions = new ConcurrentQueue<Action>();

        private void StartProcessing()
        {
            Task.Factory.StartNew(
                () =>
                    {
                        while (true)
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(1));
                            lock(_channels)
                              ProcessMessages();
                            Action additionalAction;
                            if (_additionActions.TryDequeue(out additionalAction))
                            {
                                additionalAction();
                            }
                        }
                    }, TaskCreationOptions.LongRunning);
        }

        private void ProcessMessages()
        {
            var updates = new List<StatisticEvent>();
            var name = _settings.Settings.InstanceName;
            const int maxUpdates =100;
            foreach (var typeChannels in _channels)
                foreach(var channel in typeChannels.Value)
                {
                    StatChannel.Mesg msg;
                    for (var i = 0; i < maxUpdates & channel.TryDequeue(out msg);i++)
                    {
                        switch (msg.Action)
                        {
                            case Actions.Posted:
                                updates.Add(new PostEvent(name,msg.id, typeChannels.Key, msg.Length, msg.Time));
                                break;
                            case Actions.ExternalGet:
                                if (msg.ids == null)
                                    updates.Add(new ExternalGetEvent(name, msg.id, typeChannels.Key, msg.Length, msg.Time));
                                else
                                    updates.Add(new ExternalSerialGetEvent(name, msg.ids, typeChannels.Key, msg.Length, msg.Time));                                
                                break;
                            case Actions.CacheGet:
                                updates.Add(new CachedGetEvent(name, msg.id, typeChannels.Key, msg.Length, msg.Time));
                                break;
                            case Actions.BecameUnactual:
                                updates.Add(new UnactualGetEvent(name, msg.id, typeChannels.Key, msg.Time));
                                break;
                            case Actions.RebalanceStarted:
                                updates.Add(new RebalanceEvent(name, msg.Time, typeChannels.Key, "Started"));
                                break;
                            case Actions.RebalanceQueued:
                                updates.Add(new RebalanceEvent(name, msg.Time, typeChannels.Key, "Queued"));
                                break;
                            case Actions.RebalanceEnded:
                                updates.Add(new RebalanceEvent(name, msg.Time, typeChannels.Key, "Ended", msg.Length));
                                break;
                        }
                    }
                }
            _statQueue.PutEvent(updates);
        }

        
    }
}
