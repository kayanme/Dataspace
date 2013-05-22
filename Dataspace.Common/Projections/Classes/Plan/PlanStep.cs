using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.Utility;
using Dataspace.Common.Projections.Classes.Plan;
using SetOfChildNodes = System.Collections.Generic.KeyValuePair<Dataspace.Common.Projections.Classes.ProjectionElement,System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Guid,System.Collections.Generic.IEnumerable<System.Guid>>>>;
using PackFactory = Dataspace.Common.Utility.Accumulator<Dataspace.Common.Projections.Classes.FrameNode, System.Collections.Generic.IEnumerable<System.Guid>>;


namespace Dataspace.Common.Projections.Classes.Descriptions
{

    [DebuggerDisplay("{MatchedElement.Name}-{ProducedChildElement.Name}, all:{WholeParameters}, used:{UsedParameters}, group {PriorityGroup}")]
    internal class PlanStep
    {

        private readonly PackFactory _packGetter;
        public readonly ProjectionElement MatchedElement;
        public readonly ProjectionElement ProducedChildElement;
        public readonly int PriorityGroup;
        public readonly ParameterNames WholeParameters;
        public readonly ParameterNames UsedParameters;

        public PlanStep(ProjectionElement matchedElement,
                        ProjectionElement producedChildElement,
                        ParameterNames parameterNames,
                        PackFactory accumulator,
                        ParameterNames wholeParameters,
                        int priorityGroup,
                        bool check = true)
        {
            MatchedElement = matchedElement;
            _packGetter = accumulator;
            PriorityGroup = priorityGroup;
            ProducedChildElement = producedChildElement;
            UsedParameters = parameterNames;
            WholeParameters = wholeParameters;
            _getterCheck = check;

        }

        private readonly bool _getterCheck; //проверка корректности написания ридеров. Замедляет работу, поэтому опция.

        private FrameNode CreateNewNode(FrameNode parent, Guid k)
        {
            var newDict = new Dictionary<string, object>(parent.BoundingParameters);
            if (newDict.ContainsKey(MatchedElement.Name))
                newDict[MatchedElement.Name] = k;
            else
            {
                newDict.Add(MatchedElement.Name, k);
            }
            return new FrameNode(k, ProducedChildElement, parent.Depth + 1, newDict);

        }

        /// <summary>
        /// Получает группу дочерних нод заданного шагом типа (producedChildElement) для родителя заданного типа.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        public Lazy<FrameNodeGroup> GetNextLevelNodes(FrameNodeGroup node)
        {
            var childNodes = node.Nodes.Select(k => new {parent = k, children = _packGetter.GetValue(k)}).ToArray();
            Func<FrameNodeGroup> processChildren =
                () =>
                    {
                        var groupChildren = new List<FrameNode>();
                        foreach (var pair in childNodes)
                        {
                            var children = pair.children();
                            var localChildren = children.Select(k => CreateNewNode(pair.parent, k)).ToArray();;
                            if (pair.parent.ChildNodes == null)
                                pair.parent.ChildNodes = localChildren;
                            else
                            {
                                pair.parent.ChildNodes = localChildren.Concat(pair.parent.ChildNodes).ToArray();
                            }
                            groupChildren.AddRange(localChildren);
                        }
                        var newParameters =
                            new ParameterNames(node.BoundingParameters.Concat(new[] {MatchedElement.Name}));
                        var group = new FrameNodeGroup(ProducedChildElement, groupChildren.ToArray(), newParameters);
                        return group;
                    };
            return new Lazy<FrameNodeGroup>(processChildren);
        }



    }

}
