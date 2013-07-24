using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace TwoLevelCachePerfomance.Classes
{
    [XmlRoot("DataRow",Namespace="Empty")]
    public class DataRow:Row
    {
        [XmlAttribute] 
        public int BranchChanges;
        [XmlAttribute]
        public double ProbabilitiesStability;
        [XmlAttribute]
        public double ExpectedRate;
        [XmlAttribute]
        public double Rate;
        [XmlAttribute]
        public int AverageTime;
        [XmlAttribute]
        public int C1ItemsCount;
        [XmlAttribute]
        public int C2ItemsCount;
        [XmlAttribute]
        public int C2ItemsGone;
        [XmlAttribute]
        public float GoneIntensity;
        [XmlAttribute]
        public int BranchDepth;
    }
}
