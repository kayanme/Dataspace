using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;

using Resources.Test.TestResources;

namespace Resources.Notification.Test.Resource.Level1Providers
{

    [Export(typeof(ResourcePoster))]
    internal class NotifiedElementPoster:ResourcePoster<NotifiedElement>
    {
#pragma warning disable 0649
        [Import] 
        private ResourcePool _pool;
#pragma warning restore 0649

        protected override void WriteResourceTyped(Guid key, NotifiedElement resource)
        {
           if (_pool.NotifiedElements.ContainsKey(key))
               _pool.NotifiedElements[key] = resource;
           else
               _pool.NotifiedElements.Add(key,resource);
        }

        protected override void DeleteResourceTyped(Guid key)
        {
            if (_pool.NotifiedElements.ContainsKey(key))
                _pool.NotifiedElements.Remove(key);
        }
    }
}
