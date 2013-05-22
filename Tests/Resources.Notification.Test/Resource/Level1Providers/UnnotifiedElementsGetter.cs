using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;

using Resources.Test.TestResources;

namespace Resources.Notification.Test.Resource.Level1Providers
{
    [Export(typeof(ResourceGetter))]
    internal class UnnotifiedElementGetter : ResourceGetter<UnnotifiedElement>
    {
#pragma warning disable 0649
        [Import]
        private ResourcePool _pool;
#pragma warning restore 0649
        protected override UnnotifiedElement GetItemTyped(Guid id)
        {
            if (_pool.UnnotifiedElements.ContainsKey(id))
                return _pool.UnnotifiedElements[id];
            else
                return null;
        }

        protected override IEnumerable<KeyValuePair<Guid, UnnotifiedElement>> GetItemsTyped(IEnumerable<Guid> id)
        {
            return id.Select(GetItemTyped).ToDictionary(k => k.Key);
        }
    }
}
