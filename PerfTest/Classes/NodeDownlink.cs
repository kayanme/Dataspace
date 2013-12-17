using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;

namespace PerfTest.Classes
{
    [Export(typeof(AnnouncerDownlink))]
    internal class NodeDownlink:AnnouncerDownlink
    {

        private IObserver<ResourceDescription> _cache;

        public override IDisposable Subscribe(IObserver<ResourceDescription> observer)
        {
            _cache = observer;
            return null;
        }

        public override void Dispose()
        {
            
        }

        public void Next(ResourceDescription descr)
        {
            _cache.OnNext(descr);
        }
    }
}
