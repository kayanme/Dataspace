using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common;
using Dataspace.Common.Attributes;
using Dataspace.Common.Attributes.CachingPolicies;

namespace Resources.Security.Test.SecurityResources
{
    [Resource("SecurityGroup")]
    [Serializable]
    [SimpleCaching]
    public class SecurityGroup
    {
        public IEnumerable<Guid> Groups;
    }
}
