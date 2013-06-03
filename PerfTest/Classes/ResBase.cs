using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Attributes.CachingPolicies;

namespace PerfTest.Classes
{
    [Serializable]
    [SimpleCaching]
    public class ResBase
    {
        public Guid Payload { get; set; }
    }
}
