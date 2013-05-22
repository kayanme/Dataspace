using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;

using Resources.Test.TestResources;
using Resources.Test.TestResources.SecurityResource;

namespace Resources.Security.Test.SecurityResources
{
    [Export(typeof(ResourceGetter))]
    internal class SecurityPermissionGetter:ResourceGetter<SecurityPermissions>
    {
#pragma warning disable 0649
        [Import]
        private ResourcePool _pool;
#pragma warning restore 0649
        public static bool WasChanged = false;

        protected override SecurityPermissions GetItemTyped(Guid id)
        {
            try
            {
                WasChanged = true;
                return _pool.SecurityPermissions[id];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }

        }

        protected override IEnumerable<KeyValuePair<Guid, SecurityPermissions>> GetItemsTyped(IEnumerable<Guid> id)
        {
            WasChanged = true;
            return id.Select(k => new KeyValuePair<Guid, SecurityPermissions>(k, _pool.SecurityPermissions[k])).ToArray();
        }

    }
    
}
