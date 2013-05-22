using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;

using Resources.Test.TestResources.ElementInModel;

namespace Resources.Test.TestResources
{

    [Export(typeof(ResourceQuerier))]
    public class ElementsInModelQuery : ResourceQuerier<ElementsInModel>
    {

        [IsQuery]
        public IEnumerable<Guid> ByModel(Guid element)
        {
            var el = TypedPool.Get<Element>(element);
            if (el == null)
                return DefaultValue;

            return new[]{el.Model};

        }

      

        [IsQuery]
        public IEnumerable<Guid> Uni(UriQuery query)
        {

            return new[] {Guid.Empty};

        }

    }
}
