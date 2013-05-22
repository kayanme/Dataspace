using Dataspace.Common.Attributes;
using Dataspace.Common.Attributes.CachingPolicies;


namespace Resources.Test.TestResources.ModelDeriver
{
    [CachingData("ModelDeriver")]   
    [ExplicitUpdateCaching]
    public class ModelDeriver
    {
        public string Name { get; set; }
    }
}
