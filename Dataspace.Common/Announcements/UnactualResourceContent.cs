using System;
using System.Runtime.Serialization;
using Dataspace.Common.Data;

namespace Dataspace.Common.Announcements
{
    [Serializable]
    [DataContract]
    public sealed class UnactualResourceContent:ResourceDescription
    {
        public override bool Equals(object obj)
        {
            var otherSide = obj as UnactualResourceContent;
            if (otherSide == null)
                return false;
            return otherSide.ResourceKey == ResourceKey && otherSide.ResourceName == ResourceName;
        }

        public override int GetHashCode()
        {
            return ResourceKey.GetHashCode() + ResourceName.GetHashCode();
        }
    }
}
