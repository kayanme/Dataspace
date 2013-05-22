using System;

namespace Dataspace.Common.Attributes.CachingPolicies
{

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExplicitUpdateCachingAttribute:CachingPolicyAttribute
    {
    }
}
