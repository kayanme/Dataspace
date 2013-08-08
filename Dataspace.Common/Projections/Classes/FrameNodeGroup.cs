using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Dataspace.Common.Projections.Classes.Plan;

namespace Dataspace.Common.Projections.Classes
{
    /// <summary>
    /// Текущий уровень обраотки дерева
    /// </summary>
    internal sealed class FrameNodeGroup
    {
        public readonly FrameNode[] Nodes;
        public readonly ProjectionElement MatchedElement;
        /// <summary>
        /// Набор доступных на данном уровне ограничивающих параметров.
        /// </summary>
        public readonly ParameterNames BoundingParameters;
        
        public string Name { get { return MatchedElement.Name; } }

        public FrameNodeGroup(ProjectionElement descriptor, FrameNode[] nodes, ParameterNames boundingParameters)
        {
            MatchedElement = descriptor;
            Nodes = nodes;
            BoundingParameters = boundingParameters;
            Debug.Assert(nodes.All(k => k.MatchedElement == descriptor));
            Debug.Assert(nodes.All(k => new ParameterNames(k.BoundingParameters.Select(k2=>k2.Key)) == BoundingParameters ));
        }
    }
}
