using Dataspace.Common.Attributes;
using Dataspace.Common.Attributes.CachingPolicies;


namespace Resources.Test.TestResources.ElementDeriver
{
    [CachingData("ElementDeriver")]
    [DependentCaching(typeof(Element))]
    public class ElementDeriver
    {
        public string Name { get; set; }
    }
}
