using System;

namespace Dataspace.Common.Attributes.CachingPolicies
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public class SimpleCachingAttribute:CachingPolicyAttribute
    {
    }
}
