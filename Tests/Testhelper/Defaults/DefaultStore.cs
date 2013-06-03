using System;
using System.ComponentModel.Composition;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;
using Indusoft.Test.MockresourceProviders;

namespace Indusoft.Testhelper.Defaults
{
    [Export(typeof(IResourcePosterFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Writer : IResourcePosterFactory
    {

        [Import(AllowRecomposition = true)]
        internal Store Store;

        [Import(AllowRecomposition = true)]
        internal IGenericPool Pool;

        private class Poster<T> : ResourcePoster<T> where T:class
        {
            private Store _store;
            private string _name;

            public Poster(string name, Store store)
            {
                _store = store;
                _name = name;
            }

            protected override void WriteResourceTyped(Guid key, T resource)
            {               
                _store.AddResource(resource.GetType(), key, resource);
            }

            protected override void DeleteResourceTyped(Guid key)
            {
                _store.DeleteResource(typeof(T), key);
            }
        }

        public ResourcePoster<T> CreateWriter<T>() where T : class
        {
            var name = Pool.GetNameByType(typeof(T));
            Store.AddName(typeof(T));
            return new Poster<T>(name, Store);
        }
       
    }
}
