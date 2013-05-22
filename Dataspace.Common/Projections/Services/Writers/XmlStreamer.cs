using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Projections.Services
{

    [Export(typeof(Streamer<Stream>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class XmlStreamer:Streamer<Stream>
    {

        [ImportingConstructor]
        public XmlStreamer(ProjectionWriter<Stream> writer):base(writer)
        {
            
        }
    }
}
