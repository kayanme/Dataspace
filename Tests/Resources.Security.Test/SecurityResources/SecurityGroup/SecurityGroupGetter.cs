using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;

using Resources.Test.TestResources;

namespace Resources.Security.Test.SecurityResources
{

    [Export(typeof(ResourceGetter))]
    internal class SecurityGroupGetter:ResourceGetter<SecurityGroup>
    {
#pragma warning disable 0649
        [Import]
        private ResourcePool _pool;
#pragma warning restore 0649
        public static bool WasChanged = false;

        protected override SecurityGroup GetItemTyped(Guid id)
        {
            try
            {
                WasChanged = true;
                return _pool.SecurityGroups[id];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }

        }

        protected override IEnumerable<KeyValuePair<Guid, SecurityGroup>> GetItemsTyped(IEnumerable<Guid> id)
        {
            WasChanged = true;
            return id.Select(k => new KeyValuePair<Guid, SecurityGroup>(k, _pool.SecurityGroups[k])).ToArray();
        }

    }
}
