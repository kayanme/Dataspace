using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Server.Modules.HierarchiesModule
{
    [DataContract(Name = "HierarchyQuery", Namespace = "http://tempuri.org/")]
    public class HierarchyQuery
    {
        [DataMember]
        public int AreaDepth { get; set; }

        [DataMember]
        public IEnumerable<Guid> RequiredElemens { get; set; }

        [DataMember]
        public int MaxItems { get; set; }

        [DataMember]
        public string Query { get; set; }

        public HierarchyQuery()
        {
            AreaDepth = int.MaxValue;
        }
    }
}
