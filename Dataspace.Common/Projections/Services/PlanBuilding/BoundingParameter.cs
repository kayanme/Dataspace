using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Projections.Services.PlanBuilding
{
    /// <summary>
    /// Ограничивающий параметр, формируемый на первом проходе.
    /// </summary>
    internal class BoundingParameter
    {
        public string Name;
        public int Depth;

        public class Comparer : IComparer<BoundingParameter>
        {
            public int Compare(BoundingParameter x, BoundingParameter y)
            {
                if (x.Depth != y.Depth)
                    return Comparer<int>.Default.Compare(x.Depth, y.Depth);
                return StringComparer.InvariantCultureIgnoreCase.Compare(x.Name, y.Name);
            }
        }

        public BoundingParameter(string name, int depth)
        {
            Name = name;
            Depth = depth;
        }
    }
}
