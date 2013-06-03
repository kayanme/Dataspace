using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;

namespace PerfTest.Classes
{
    [Export(typeof(IResourceGetterFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class Downgetter:IResourceGetterFactory
    {
        private CompositionContainer _cont;

        private class Getter<T>:ResourceGetter<T> where T:class
        {
            private ITypedPool _pool;

            protected override T GetItemTyped(Guid id)
            {
                return _pool.Get<T>(id);
            }

            public Getter(ITypedPool pool)
            {
                _pool = pool;
            }
        }

        public ResourceGetter<T> CreateGetter<T>() where T : class
        {
           return new Getter<T>(_cont.GetExportedValue<ITypedPool>());
        }

        public Downgetter(CompositionContainer cont)
        {
            _cont = cont;
        }
    }

    [Export(typeof(IResourcePosterFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class Downposter : IResourcePosterFactory
    {
        private CompositionContainer _cont;

        private class Poster<T> : ResourcePoster<T> where T : class
        {
            private ITypedPool _pool;

          

            public Poster(ITypedPool pool)
            {
                _pool = pool;
            }

            protected override void WriteResourceTyped(Guid key, T resource)
            {
                _pool.Post(key,resource);
            }

            protected override void DeleteResourceTyped(Guid key)
            {
               _pool.Post<T>(key,null);
            }
        }

        public Downposter(CompositionContainer cont)
        {
            _cont = cont;
        }

        public ResourcePoster<T> CreateWriter<T>() where T : class
        {
            return new Poster<T>(_cont.GetExportedValue<ITypedPool>());
        }
    }

    [Export(typeof(IResourceQuerierFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class LinkQuery : IResourceQuerierFactory
    {
        private readonly CompositionContainer _cont;

        public FormedQuery CreateQuerier(string type, string nmspc, string[] args)
        {
            var pool = _cont.GetExportedValue<IGenericPool>();
            return pars =>
                       {
                           var query = new UriQuery(args.Zip(pars, 
                               (a, b) => new KeyValuePair<string, string>(a, b.ToString())));
                           return pool.Get(type, query, nmspc);
                       };

        }

        public LinkQuery(CompositionContainer cont)
        {
            _cont = cont;
        }
    }
}
