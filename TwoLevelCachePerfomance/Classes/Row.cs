using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TwoLevelCachePerfomance.Classes
{
    public abstract class Row
    {
        [XmlIgnore]
        public int Iteration;
    }
}
