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

        public Guid? ResourceAffinity1 { get; set; }

        public Guid? ResourceAffinity2 { get; set; }

        public string NodeAffinity { get; set; }
    }
}
