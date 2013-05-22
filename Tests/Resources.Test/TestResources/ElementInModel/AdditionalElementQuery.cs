using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;


namespace Resources.Test.TestResources.ElementInModel
{
    [Export(typeof(ResourceQuerier))]
    public class AdditionalElementQuery : ResourceQuerier<Element>
    {
        [IsQuery]
        public IEnumerable<Guid> ByElInModel(Guid elementsInModel)
        {
            var el = TypedPool.Get<ElementsInModel>(elementsInModel);
            return el.Elements;
        }
    }
}
