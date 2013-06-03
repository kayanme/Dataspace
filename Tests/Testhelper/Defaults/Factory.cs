using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;
using Indusoft.Test.MockresourceProviders;

namespace Indusoft.Testhelper.Defaults
{

    [Export(typeof(IResourceGetterFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class Factory:IResourceGetterFactory
    {

        [Import(AllowRecomposition = true)] 
        internal Store Store;

        private class Getter<T>:ResourceGetter<T> where T:class
        {
            private Store _store;
            
            protected override T GetItemTyped(Guid id)
            {
                return (T)_store.GetResource(typeof(T), id);
            }

            protected override IEnumerable<KeyValuePair<Guid, T>> GetItemsTyped(IEnumerable<Guid> id)
            {
                return id.Select(k => new KeyValuePair<Guid, T> ( k, GetItemTyped(k) )).ToArray();
            }

            public Getter(Store store)
            {
                _store = store;
               
            }
        }

        public ResourceGetter<T> CreateGetter<T>() where T : class
        {
          
            Store.AddName(typeof(T));
            return new Getter<T>( Store);
        }
    }
}
