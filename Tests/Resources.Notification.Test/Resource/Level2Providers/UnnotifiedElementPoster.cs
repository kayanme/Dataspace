using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;

using Resources.Test.TestResources;

namespace Resources.Notification.Test.Resource.Level2Providers
{
    [Export(typeof(ResourcePoster))]
    internal class UnnotifiedElementPoster : ResourcePoster<UnnotifiedElement>
    {
#pragma warning disable 0649
        [Import]
        private LinkToTransporter _pool;
#pragma warning restore 0649
        protected override void WriteResourceTyped(Guid key, UnnotifiedElement resource)
        {
           _pool.Transporter.Post(key, resource);
        }

        protected override void DeleteResourceTyped(Guid key)
        {
            _pool.Transporter.Post<UnnotifiedElement>(key, null);
        }
    }
}
