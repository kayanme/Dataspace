using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Projections.Classes.Descriptions;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.ServiceResources;
using Dataspace.Common.Utility;
using Provider = Dataspace.Common.Utility.Accumulator<
                                  Dataspace.Common.Projections.Classes.FrameNode,
                                  System.Collections.Generic.IEnumerable<System.Guid>>;
using GroupStorage = System.Collections.Concurrent.ConcurrentDictionary<Dataspace.Common.Projections.Classes.FrameNode, System.Collections.Generic.IEnumerable<System.Guid>>;

namespace Dataspace.Common.Projections.Classes
{   
    internal  class FramingPlan
    {
        private readonly ProjectionElement _rootElement;

        private readonly bool _checkMode;

        private List<GroupStorage> _storages = new List<GroupStorage>();

        private Dictionary<string,Func<string,object>> _primaryConversions 
            = new Dictionary<string, Func<string, object>>(StringComparer.InvariantCultureIgnoreCase);

        protected CommendationCollection _commendations = new CommendationCollection();

        private IEnumerable<FrameNodeGroup> FilterByDepth(IEnumerable<FrameNodeGroup> nodes,int maxDepth)
        {
            Contract.Requires(nodes != null);
            Contract.Ensures(Contract.Result<FrameNodeGroup[]>() != null);
            if (maxDepth == -1)
                return nodes;
            else
            {
                var groups =
                    nodes.Select(k => new FrameNodeGroup(k.MatchedElement, k.Nodes.Where(k2 => k2.Depth <= maxDepth).ToArray(),k.BoundingParameters))
                         .Where(k => k.Nodes.Any())
                         .ToArray();
                foreach(var node in nodes.SelectMany(k=>k.Nodes).Where(k=>k.Depth > maxDepth))
                {
                    node.ChildNodes = new FrameNode[0];
                    if (!node.MatchedElement.DownRelations.Any())
                          node.DefinitlyHasNoChildren = true;
                }
                return groups;
            }
        }

        private IEnumerable<FrameNodeGroup> FilterByCount(IEnumerable<FrameNodeGroup> nodes)
        {
            Contract.Requires(nodes != null);
            Contract.Ensures(Contract.Result<FrameNodeGroup[]>() != null);         
           
                var groups =
                    nodes.Where(k => k.Nodes.Any())
                         .ToArray();
              
                return groups;            
        }      

      
        public ProjectionFrame MakeFrame(Guid id, int maxDepth, Dictionary<string, object> parameters)
        {
            var root = new FrameNode(id, _rootElement,1,parameters);
            IEnumerable<FrameNodeGroup> currentLevel = new[]
                                   {
                                       new FrameNodeGroup(_rootElement, 
                                           new[] {root}, 
                                           new ParameterNames(parameters.Select(k=>k.Key)))
                                   };
            if (maxDepth > 1 || maxDepth == -1)
            {
                while (currentLevel.Any())
                {

                    currentLevel = _commendations.GetNewGroups(currentLevel);                  
                    currentLevel = FilterByDepth(currentLevel, maxDepth);
                    currentLevel = FilterByCount(currentLevel);
                }
            }
            else
            {
                root.ChildNodes = new FrameNode[0];
            }
            foreach(var storage in _storages)
            {
                storage.Clear();
            }
            return new ProjectionFrame(root);
        }

        public FramingPlan(ProjectionElement root,bool checkMode = true)
        {
            _rootElement = root;
            _checkMode = checkMode;
        }

        public void AddConversion(string parameter,Func<string,object> conv)
        {
            if (!_primaryConversions.ContainsKey(parameter))
                _primaryConversions.Add(parameter,conv);
        }

        protected virtual PlanStep CreateStep(ProjectionElement parent, ProjectionElement child, ParameterNames pars, ParameterNames allPars, Provider provider, int priorGroup,Query func)
        {
            var step = new PlanStep(parent, child, pars, provider, allPars, priorGroup, _checkMode);
            return step;
        }

        public void AddNewStep(ProjectionElement parent, ProjectionElement child, ParameterNames pars, ParameterNames allPars, Provider provider, int priorGroup, Query func)
        {
            var step = CreateStep(parent, child, pars, allPars,provider, priorGroup,func);
            _commendations.AddNewStep(step);
        }

        public void AddTempStorage(GroupStorage storage)
        {
            _storages.Add(storage);
        }

        public Dictionary<string,object> ConverseArguments(Dictionary<string,string> args)
        {
            //конверсии для параметра не будет, если он вообще не используется в проекции, но знать о нем на каждом шаге надо все равно, поэтому оставляем такой без конверсии
            return args.ToDictionary(k => k.Key, k =>_primaryConversions.ContainsKey(k.Key)? _primaryConversions[k.Key](k.Value):k.Value);
        }
        
    }

   

}
