using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.Projections.Classes;

namespace Dataspace.Common.Projections.Services.PlanBuilding
{
    /// <summary>
    /// Узел, содержащий информацию о ребре при первом обходе - отец-родитель, отношение
    /// и список определенных параметров на этом уровне.
    /// </summary>
    internal class PrimaryFrameNode
    {
        public ProjectionElement Parent;
        public ProjectionElement Current;
        public Relation Relation;
        public BoundingParameter[] OrderedParameters;
    }
}
