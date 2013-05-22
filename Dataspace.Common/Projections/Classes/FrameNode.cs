using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Projections.Classes
{
    [DebuggerDisplay("{MatchedElement.Name}:{Key}, {Depth}")]
    internal class FrameNode
    {
        public readonly Guid Key;

        public readonly ProjectionElement MatchedElement;

        public readonly Dictionary<string, object> BoundingParameters;

        public FrameNode[] ChildNodes;

        public readonly int Depth;

        public bool DefinitlyHasNoChildren;

        public FrameNode(Guid key, ProjectionElement type, int depth, Dictionary<string, object> boundingParameters)
        {
           
            Key = key;
            Depth = depth;
            MatchedElement = type;
            BoundingParameters = new Dictionary<string, object>(boundingParameters,StringComparer.InvariantCultureIgnoreCase);
            
        }


       public static IEqualityComparer<FrameNode> InPlanComparer = new FrameEqComparer();

        private class FrameEqComparer :EqualityComparer<FrameNode>
        {
                      
            public override bool Equals(FrameNode x, FrameNode y)
            {
                 
                if (x == null || y == null)
                    return false;

                if (x.Key != y.Key)
                    return false;

                if (x.BoundingParameters.Count != y.BoundingParameters.Count)
                    return false;

                var xKeys = x.BoundingParameters.Keys.OrderBy(k=>k).ToArray();
                var yKeys = y.BoundingParameters.Keys.OrderBy(k => k).ToArray();

                if (!xKeys.SequenceEqual(yKeys))
                    return false;

                var xValues = xKeys.Select(k => x.BoundingParameters[k]);
                var yValues = yKeys.Select(k => y.BoundingParameters[k]);

                if (!xValues.SequenceEqual(yValues))
                    return false;

                return true;
            }

            public override int GetHashCode(FrameNode obj)
            {
                var dictKey = 0;
                unchecked
                {
                    foreach (KeyValuePair<string, object> k in obj.BoundingParameters)
                        dictKey += k.GetHashCode();
                }
                return (dictKey << 16) & (obj.Key.GetHashCode() & 0xFFFF);
            }
        }
    }
}
