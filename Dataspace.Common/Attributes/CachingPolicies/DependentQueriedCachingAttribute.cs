using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace Dataspace.Common.Attributes.CachingPolicies
{
    public class DependentQueriedCachingAttribute:CachingPolicyAttribute
    {
         public Type ParentType { get; private set; }

         public DependentQueriedCachingAttribute(Type parentType)
         {
            Contract.Requires(GetCustomAttribute(parentType, typeof(ResourceAttribute)) as ResourceAttribute != null);
            var res = GetCustomAttribute(parentType, typeof (ResourceAttribute)) as ResourceAttribute;
            Debug.Assert(res != null, "Родительский тип не является ресурсом");
            if (res == null)
                throw new ArgumentException("Родительский тип не является ресурсом");

            ParentType = parentType;
         }
    }
}
