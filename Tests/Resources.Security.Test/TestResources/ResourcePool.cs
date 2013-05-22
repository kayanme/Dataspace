using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Resources.Security.Test.SecurityResources;
using Resources.Test.TestResources.SecurityResource;

namespace Resources.Test.TestResources
{
    internal class ResourcePool
    {
        internal Dictionary<Guid, Model> Models = new Dictionary<Guid, Model>();
        internal Dictionary<Guid, Element> Elements = new Dictionary<Guid, Element>();
        internal Dictionary<Guid, SecurityGroup> SecurityGroups = new Dictionary<Guid, SecurityGroup>();
        internal Dictionary<Guid, SecurityPermissions> SecurityPermissions = new Dictionary<Guid, SecurityPermissions>();
    }
}
