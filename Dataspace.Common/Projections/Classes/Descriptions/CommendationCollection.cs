using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Dataspace.Common.Projections.Classes.Plan;

namespace Dataspace.Common.Projections.Classes.Descriptions
{

  
    internal class CommendationCollection
    {
    

        //очередь используется для подчеркивания очередности проверок, значения из нее никогда не удаляются
        private readonly List<PlanStep> _possibleSteps = new List<PlanStep>();


        internal IEnumerable<PlanStep>  PossibleSteps
        {
            get { return _possibleSteps; }
        }

        /// <summary>
        /// Действие, которое надо выполнить с данной коллекцией узлов.
        /// </summary>
        /// <param name="groups">Группа узлов.</param>
        /// <returns>Действие</returns>       
        private IEnumerable<Tuple<FrameNodeGroup,PlanStep>> GetGroupsForProcessing(IEnumerable<FrameNodeGroup> groups)
        {
            
            Contract.Requires(groups != null);
            var allSteps = groups.SelectMany(k =>
                                             _possibleSteps.Where(k2 => k2.MatchedElement == k.MatchedElement
                                                                     && k2.WholeParameters == 
                                                                     new ParameterNames(k.BoundingParameters.Except(new[]{k.Name},StringComparer.InvariantCultureIgnoreCase)))
                                                           .Select(k2 => new
                                                                   {
                                                                       group = k,
                                                                       step = k2
                                                                   })
                );
            var prioritisedSteps = allSteps.GroupBy(k => k.step.PriorityGroup)
                                           .OrderBy(k => k.Key)
                                           .First();
            return prioritisedSteps.Select(k=>Tuple.Create(k.group,k.step)).ToArray();
        }

        /// <summary>
        /// Действие, которое надо выполнить с данной коллекцией узлов.
        /// </summary>
        /// <param name="groups">Группа узлов.</param>
        /// <returns>Действие</returns>       
        private IEnumerable<FrameNodeGroup> GetTerminatingGroups(IEnumerable<FrameNodeGroup> groups)
        {

            Contract.Requires(groups != null);
            var termGroups = groups.Where(k =>
                                             !_possibleSteps.Any(k2 => k2.MatchedElement == k.MatchedElement
                                                                    && k2.WholeParameters == 
                                                                    new ParameterNames(k.BoundingParameters.Except(new[]{k.Name},StringComparer.InvariantCultureIgnoreCase)))
                                                          );

            return termGroups.ToArray();
        }     

        private void CloseTerminatedGroups(IEnumerable<FrameNodeGroup> groups)
        {
            foreach (var frameNodeGroup in groups)
            {
                foreach (var node in frameNodeGroup.Nodes)
                {
                    node.ChildNodes = new FrameNode[0];
                }
            }
        }

        /// <summary>
        /// Действие, которое надо выполнить с данной коллекцией узлов.
        /// </summary>
        /// <param name="groups">Группа узлов.</param>
        /// <returns>Действие</returns>
        /// <remarks>Виртуальный исключительно для тестирования</remarks>
        public virtual IEnumerable<FrameNodeGroup> GetNewGroups(IEnumerable<FrameNodeGroup> groups)
        {
            Contract.Requires(groups!=null);
            var terminatingGroups = GetTerminatingGroups(groups);
            CloseTerminatedGroups(terminatingGroups);
            var notTerminatingGroups = groups.Except(terminatingGroups).ToArray();
            if (!notTerminatingGroups.Any())
                return new FrameNodeGroup[0];
            var groupsForProcessing = GetGroupsForProcessing(notTerminatingGroups).ToArray();
            var notProcessedYetGroups = notTerminatingGroups.Except(groupsForProcessing.Select(k => k.Item1));
            var nextLevelNodes = groupsForProcessing.Select(k => k.Item2.GetNextLevelNodes(k.Item1)).ToArray();
            return nextLevelNodes.Select(k => k.Value).Concat(notProcessedYetGroups).ToArray();
        }     
        
        internal void AddNewStep(PlanStep step)
        {
            _possibleSteps.Add(step);
        }
    }
}
