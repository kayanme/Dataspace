using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;

using Resources.Test.TestResources;

namespace Resources.Notification.Test.Resource.Level2Providers
{
    [Export(typeof(ResourceGetter))]
    public class UnnotifiedElementGetter : ResourceGetter<UnnotifiedElement>
    {
#pragma warning disable 0649
        [Import]
        private LinkToTransporter _pool;
#pragma warning restore 0649
        protected override UnnotifiedElement GetItemTyped(Guid id)
        {
            return  _pool.Transporter.Get<UnnotifiedElement>(id,IsTracking);
        }

        protected override IEnumerable<KeyValuePair<Guid, UnnotifiedElement>> GetItemsTyped(IEnumerable<Guid> id)
        {
            return id.Select(GetItemTyped).ToDictionary(k => k.Key);
        }
    }
}
