using System;
using Dataspace.Common;
using Dataspace.Common.Attributes;
using Dataspace.Common.Attributes.CachingPolicies;

namespace Resources.Test.TestResources
{
    [Resource("Element")]
    [Serializable]
    [SimpleCaching]
    public class Element
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
        public Guid Model { get; set; }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            var eq = obj as Element;
            return Key == eq.Key && Name == eq.Name && Model == eq.Model;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode() + Name.GetHashCode() + Model.GetHashCode();
        }
    }
}
