using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;


namespace Resources.Test.Providers
{
    [Export(typeof(IResourceQuerierFactory))]
    internal class Querier:IResourceQuerierFactory
    {

        private class Q<T> : ResourceQuerier<T>
        {

            [IsQuery]
            public IEnumerable<Guid> Universal(UriQuery t)
            {
                return null;
            }
        }

        public ResourceQuerier<T> CreateQuerier<T>() where T : class
        {
            return new Q<T>();
        }
    }
}
