using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;
using Indusoft.Test.MockresourceProviders;

namespace Indusoft.Testhelper.Defaults
{
    [Export(typeof(QueryProcessor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class TestQueryProcessor : QueryProcessor
    {

        [Import]
        private Store Store;

        [Import]
        private IGenericPool _cachier;

        protected override IEnumerable<T> QueryItems<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            var name = _cachier.GetNameByType(typeof (T));
            Thread.Sleep(Store.GetDelay);
            var compQuery = predicate.Compile();
            if (!Store._caches.ContainsKey(typeof(T)))
               return new T[0];
          
            lock (Store._caches[typeof (T)])
                return Store._caches[typeof (T)].Select(k => k.Value as T).Where(compQuery).ToArray();
        }




    }
}
