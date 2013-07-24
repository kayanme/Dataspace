using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.Announcements;
using Dataspace.Common.ClassesForImplementation;

namespace Dataspace.Common.Data
{
    public class DataRecord
    {
        public UnactualResourceContent Content { get; internal set; }
        public object Resource { get; internal set; }
        internal Action<Guid, object> Poster { get; set; }
     
    }
}
