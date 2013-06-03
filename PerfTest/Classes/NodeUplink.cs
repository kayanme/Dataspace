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

    [Export(typeof(AnnouncerUplink))]
    internal class NodeUplink:AnnouncerUplink
    {

        public List<NodeDownlink> Subscribers = new List<NodeDownlink>();

        public override void OnNext(ResourceDescription value)
        {
            foreach (var announcerDownlink in Subscribers)
            {
                announcerDownlink.Next(value);
            }
        }

        public override void OnError(Exception error)
        {            
        }

        public override void OnCompleted()
        {            
        }
        
    }
}
