using System;
using Dataspace.Common;
using Dataspace.Common.Attributes;
using Dataspace.Common.Attributes.CachingPolicies;

namespace Resources.Test.TestResources
{
    [Resource("Model")]
    [ExplicitUpdateCaching]
    [Serializable]
    public class Model
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
        public Guid Element { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            var eq = obj as Model;
            return Key == eq.Key && Name == eq.Name && Element == eq.Element;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode() + Name.GetHashCode() + Element.GetHashCode();
        }
    }
}
