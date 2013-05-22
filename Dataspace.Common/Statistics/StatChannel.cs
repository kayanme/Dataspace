using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.Statistics;

namespace Dataspace.Common.Statistics
{

    [Export(typeof(IStatChannel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class StatChannel:IStatChannel
    {

#pragma warning disable 0649
        [Import]
        private StatisticsCollector _collector;
#pragma warning restore 0649

        internal struct Mesg
        {
            public Guid id;
            public Guid[] ids;
            public Actions Action;
            public DateTime Time;
            public TimeSpan Length;
        }

        private ConcurrentQueue<Mesg> _queue = new ConcurrentQueue<Mesg>();



        public void Register(string resourceType)
        {            
            _collector.RegisterChannel(resourceType,_queue);
        }

        public void SendMessageAboutOneResource(Guid id, Actions action,TimeSpan length = default(TimeSpan))
        {
           _queue.Enqueue(new Mesg{Action = action,id = id,Time = DateTime.Now,Length = length});
        }

        public void SendMessageAboutMultipleResources(Guid[] ids, Actions action, TimeSpan length = default(TimeSpan))
        {
            _queue.Enqueue(new Mesg { Action = action, ids = ids, Time = DateTime.Now, Length = length });
        }
    }
}
