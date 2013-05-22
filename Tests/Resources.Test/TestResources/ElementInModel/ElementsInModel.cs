using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.Attributes;
using Dataspace.Common.Attributes.CachingPolicies;


namespace Resources.Test.TestResources.ElementInModel
{

    [CachingData("ElementsInModel")]
    [DependentQueriedCaching(typeof(Element))]
    public class ElementsInModel
    {
        public IEnumerable<Guid> Elements { get; set; }
    }
}
