using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common;
using Dataspace.Common.Attributes;
using Dataspace.Common.Attributes.CachingPolicies;

namespace Resources.Test.TestResources.SecurityResource
{
    [Resource("SecurityPermissions")]
    [Serializable]
    [SimpleCaching]
    public class SecurityPermissions
    {
        public IEnumerable<Guid> AllowedForRead;

        public IEnumerable<Guid> AllowedForWrite;
    }
}
