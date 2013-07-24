using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Utility;

namespace PerfTest.Classes
{
    [Export(typeof(IResourceGetterFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class Downgetter:IResourceGetterFactory
    {
        private CompositionContainer _cont;

        private TimeSpan _callDelay;

        private class Getter<T>:ResourceGetter<T> where T:class
        {
            private ITypedPool _pool;

            private TimeSpan _callDelay;

            protected override T GetItemTyped(Guid id)
            {
                SpinWait.SpinUntil(() => false, _callDelay);
                return _pool.Get<T>(id);
            }

            protected override IEnumerable<KeyValuePair<Guid, T>> GetItemsTyped(IEnumerable<Guid> id)
            {
                SpinWait.SpinUntil(() => false, _callDelay);
                return id.Zip(_pool.Get<T>(id),(k,v)=>new KeyValuePair<Guid, T>(k,v)).ToArray();
            }

            public Getter(ITypedPool pool,TimeSpan callDelay)
            {
                _callDelay = callDelay;
                _pool = pool;
            }
        }

        public ResourceGetter<T> CreateGetter<T>() where T : class
        {
           return new Getter<T>(_cont.GetExportedValue<ITypedPool>(),_callDelay);
        }

        public Downgetter(CompositionContainer cont,TimeSpan callDelay)
        {
            _cont = cont;
            _callDelay = callDelay;
        }
    }

    [Export(typeof(IResourcePosterFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class Downposter : IResourcePosterFactory
    {
        private CompositionContainer _cont;

        
        private TimeSpan _callDelay;

        private class Poster<T> : ResourcePoster<T> where T : class
        {
            private ITypedPool _pool;

          
            private TimeSpan _callDelay;

            public Poster(ITypedPool pool,TimeSpan callDelay)
            {
                _pool = pool;
                _callDelay = callDelay;
            }

            protected override void WriteResourceTyped(Guid key, T resource)
            {
                SpinWait.SpinUntil(() => false, _callDelay);
                _pool.Post(key,resource);
            }

            protected override void DeleteResourceTyped(Guid key)
            {
                SpinWait.SpinUntil(() => false, _callDelay);
               _pool.Post<T>(key,null);
            }
        }

        public Downposter(CompositionContainer cont,TimeSpan callDelay)
        {
            _cont = cont;
            _callDelay = callDelay;
        }

        public ResourcePoster<T> CreateWriter<T>() where T : class
        {
            return new Poster<T>(_cont.GetExportedValue<ITypedPool>(),_callDelay);
        }
    }

    [Export(typeof(IResourceQuerierFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class LinkQuery : IResourceQuerierFactory
    {
        private readonly CompositionContainer _cont;
        private readonly TimeSpan _callDelay;

        public FormedQuery CreateQuerier(string type, string nmspc, string[] args)
        {
            var pool = _cont.GetExportedValue<IGenericPool>();
            return pars =>
                       {
                           var query = new UriQuery(args.Zip(pars, 
                               (a, b) => new KeyValuePair<string, string>(a, b.ToString())));
                           SpinWait.SpinUntil(() => false, _callDelay);
                           return pool.Get(type, query, nmspc);
                       };

        }

        public LinkQuery(CompositionContainer cont,TimeSpan callDelay)
        {
            _cont = cont;
            _callDelay = callDelay;
        }
    }
}
