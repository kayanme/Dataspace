using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;

using Resources.Test.TestResources;

namespace Resources.Notification.Test.Resource.Level1Providers
{

    
    [Export]
    [Export(typeof(ResourceGetter))]
    public class NotifiedElementGetter:ResourceGetter<NotifiedElement>
    {
#pragma warning disable 0649
        [Import]
        private ResourcePool _pool;
#pragma warning restore 0649
        protected override NotifiedElement GetItemTyped(Guid id)
        {
            if (_pool.NotifiedElements.ContainsKey(id))
                return _pool.NotifiedElements[id];
            else
                return null;
        }
   
    }
}
