using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Projections.Classes;
using Dataspace.Common.Projections.Classes.Descriptions;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.Projections.Services;
using Dataspace.Common.Projections.Services.PlanBuilding;
using Dataspace.Common.Services;
using Dataspace.Common.Utility;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.Services;
using Dataspace.Common.Utility;
using Dataspace.Common.Projections.Classes.Utilities;
using Getter=System.Func<System.Collections.Generic.IEnumerable<System.Guid>,
                         System.Collections.Generic.Dictionary<string,object>,
                         System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Guid, 
                                                                                                        System.Collections.Generic.IEnumerable<System.Guid>>>>;


namespace Dataspace.Common.Projections
{

    [Export,PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class FramingPlanBuilder
    {
        #region Imports

#pragma warning disable 0649
        [Import]
        private SettingsHolder _settingsHolder;
#pragma warning restore 0649

        #endregion

        private object _lock = new object();
          

        internal class TestFramingPlanBuilder
        {
            
            public IEnumerable<PlanStep> CheckVariantsForNodes(ProjectionElement root, int depth, ParameterNames parameters)
            {
                var testBuilder = new FramingPlanBuilder();
                var plan = new TestFramingPlan(root); 
                testBuilder.CheckNodeVariants(plan, root, depth, parameters);
                return plan.ReturnPlanningResults();
            }

            public Getter FormGetter(Relation relation, BoundingParameter[] parameters, out ParameterNames parameterNames)
            {
                var testBuilder = new FramingPlanBuilder();
                ResourceQuerier.BaseFuncWithSortedArgs query;
                return testBuilder.FormGetterByRelation(relation, parameters,out parameterNames,out query);
            }
        }

        private Getter FormGetterByRelation(Relation relation, BoundingParameter[] parameters, out ParameterNames parameterNames,out ResourceQuerier.BaseFuncWithSortedArgs query)
        {
            Getter getter;
            if (relation.HasTrivialQuery)
            {
                getter =
                    (keys, parPairs) =>
                    keys.Select(k => new KeyValuePair<Guid, IEnumerable<Guid>>(k, new[] {k})).ToArray();
                parameterNames = new ParameterNames(new string[0]);
                query = null;
            }
            else
            {
                var targetQuery =
                    relation.SeriaQueries.OfType<ResourceQuerier.BaseFuncWithSortedArgs>()
                        .Concat(relation.Queries)
                        .Concat(relation.SeriaQueriesFromPhysicalSpace)
                        .Concat(relation.QueriesFromPhysicalSpace)
                        .SelectTheBestQuery(relation.ParentElement,ref parameters)
                        .FirstOrDefault();

                if (targetQuery == null)
                    throw new InvalidOperationException(
                        string.Format("Нет запроса для построения дерева: род. {0}, доч. {1} ",
                                      relation.ParentElement.Name,
                                      relation.ChildElement.Name));

                query = targetQuery;
                if (targetQuery is ResourceQuerier.SeriesFuncWithSortedArgs)
                {
                    var t = targetQuery as ResourceQuerier.SeriesFuncWithSortedArgs;
                    getter = (keys, parPairs) =>
                                 {
                                     var parValues = t.Args.Select(k => parPairs[k]).ToArray();
                                     return t.UnconversedFunction(keys, parValues);
                                 };
                }
                else
                {
                    var t = targetQuery as ResourceQuerier.FuncWithSortedArgs;
                    getter = (keys, parPairs) =>
                                 {
                                     Func<Guid, object[]> parValues =
                                         resKey =>
                                         t.Args.Select(
                                             k =>
                                             String.Equals(k, relation.ParentElement.Name, StringComparison.InvariantCultureIgnoreCase)
                                                 ? resKey
                                                 : parPairs[k])
                                             .ToArray();
                                     return
                                         keys.Select(
                                             k =>
                                             new KeyValuePair<Guid, IEnumerable<Guid>>(k,
                                                                                       t.UnconversedArgsFunction(
                                                                                           parValues(k)))).ToArray();
                                 };
                   
                }

                parameterNames = new ParameterNames(targetQuery.Args.Where(k=>!string.Equals(k,relation.ParentElement.Name,StringComparison.InvariantCultureIgnoreCase)));
            }
            return getter;
        }

        private string GetHashForElement(ProjectionElement element)
        {
            return string.Intern(string.Format("{0}-{1}:{2}", element.Name, element.SchemeType,element.GetHashCode()));
        }

        private string GetHashForElementAndParameters(ProjectionElement element, ParameterNames orderedParameters)
        {
            return string.Intern(string.Join(";", new[]{GetHashForElement(element)}.Concat(orderedParameters)));
        }

        private IEnumerable<PrimaryFrameNode> MakeAllPossibleWaysRec(List<string> processed, ProjectionElement element, SortedSet<BoundingParameter> orderedParameters, int depth)
        {
            if (depth == int.MinValue)
                throw new InvalidOperationException("Ошибка при построении плана - слишком глубокая схема для разбора");
            if (depth == 0)
                return new PrimaryFrameNode[0];

            #region Проверка - был ли проверен текущий элемент, и добавление его к проверенным

            var hash = GetHashForElementAndParameters(element, new ParameterNames(orderedParameters.Select(k => k.Name)));

           
                if (processed.Contains(hash))
                    return new PrimaryFrameNode[0];

                processed.Add(hash);
            

            #endregion

            #region Добавление текущего элемента к родительским параметрам или обнуление глубины уже существующего параметра с данным именем

            foreach (var p in orderedParameters)
            {
                p.Depth++;
            }

            var parameter = orderedParameters.FirstOrDefault(k => k.Name == element.Name);
            int oldDepth;
            if (parameter == null)
            {
                parameter = new BoundingParameter(element.Name, 1);
                orderedParameters.Add(parameter);
                oldDepth = 0;
            }
            else
            {
                oldDepth = parameter.Depth;
                parameter.Depth = 1;
            }

            #endregion

            #region Обход детей дочерних нод

            var childElements = element.DownRelations
                .Select(k => new
                                 {
                                     relation = k,
                                     k.ChildElement,
                                     children =
                                 MakeAllPossibleWaysRec(processed, k.ChildElement, orderedParameters, depth - 1)
                                 }).ToArray();

            #endregion



            processed.Remove(hash);

            if (oldDepth == 0)
                orderedParameters.Remove(parameter);
            else
            {
                parameter.Depth = oldDepth;
            }


            #region Формирование списка нод вместе с дочерними

            var allNodes = childElements.Select(k =>
                                                new PrimaryFrameNode
                                                    {
                                                        Current = k.ChildElement,
                                                        OrderedParameters =
                                                            orderedParameters.Where(k2=>!StringComparer.InvariantCultureIgnoreCase.Equals(k2.Name,element.Name))
                                                                             .Select(k2 => new BoundingParameter(k2.Name, k2.Depth))
                                                                             .ToArray(),
                                                        Parent = element,
                                                        Relation = k.relation
                                                    })
                .Concat(childElements.SelectMany(k => k.children))
                .ToArray();

            #endregion


            foreach (var p in orderedParameters)
            {
                p.Depth--;
            }
          
            return allNodes;
        }

        private IEnumerable<PrimaryFrameNode> MakeAllPossibleWays(ProjectionElement root, ParameterNames orderedParameters, int depth)
        {            
            var processed = new List<string>();
            var parameters = new SortedSet<BoundingParameter>(
                orderedParameters.Select(k=>new BoundingParameter(k,0)),//-1 - потому что при старте корня они сразу поимеют глубину 0.
               new BoundingParameter.Comparer());
            var ways = MakeAllPossibleWaysRec(processed, root,parameters, depth);
            Debug.Assert(processed.Count == 0);
            return ways;
        }

        private SecondaryFrameNode FormSecondaryOnFirst(PrimaryFrameNode node)
        {
            ParameterNames parameterNames;
            ResourceQuerier.BaseFuncWithSortedArgs query;
            var getter = FormGetterByRelation(node.Relation, node.OrderedParameters,out parameterNames,out query);
            var secondary = new SecondaryFrameNode
                                {
                                    Current = node.Current,
                                    Parent = node.Parent,
                                    Getter = getter,
                                    AllParameters = new ParameterNames(node.OrderedParameters.Select(k=>k.Name)),
                                    UsingParameters = new ParametersMapping(parameterNames),
                                    Query = query
                                };
            return secondary;
        }

        private IEnumerable<SecondaryFrameNode> FindMatchedQueries(IEnumerable<PrimaryFrameNode> nodes)
        {
            return nodes.Select(FormSecondaryOnFirst).ToArray();
        }




        private IEnumerable<SecondaryFrameNode> FillPriorityGroupAndCleanDuplicates(IEnumerable<SecondaryFrameNode> nodes)
        {
            var counts =
                nodes.GroupBy(k => k, SecondaryFrameNode.EqualityComparer)
                    .Select(k => new {k.Key, count = k.Count()})
                    .ToArray();
            foreach(var node in counts)
            {
                node.Key.PriorityGroup = node.count;
            }
            return counts.Select(k => k.Key);

        }

        private Accumulator<FrameNode, IEnumerable<Guid>> GetAccumulatorForSecondary(FramingPlan plan, SecondaryFrameNode node, AccumulatorFactory accumulator)
        {
            return accumulator.GetOrCreateAccumulator(plan,node.Getter,
                                                      node.Parent, 
                                                      node.Current,
                                                      node.UsingParameters.ParameterNames);
        }

        private void MakePlanStepFromSecondary(FramingPlan plan, AccumulatorFactory accumulator, SecondaryFrameNode node)
        {
            plan.AddNewStep(node.Parent,
                            node.Current,
                            node.UsingParameters.ParameterNames,
                            node.AllParameters,
                            GetAccumulatorForSecondary(plan,node,accumulator),
                            node.PriorityGroup,
                            node.Query);
        }

        private void CheckNodeVariants(FramingPlan plan, ProjectionElement element, int depth, ParameterNames orderedParameters)
        {
            Contract.Requires(element != null);
            Contract.Requires(plan != null);
            Contract.Requires(orderedParameters != null);
            
            var nodes = MakeAllPossibleWays(element, orderedParameters, depth);
            var nodesWithQueries = FindMatchedQueries(nodes);
            foreach (var secondaryFrameNode in nodesWithQueries)
            {
                foreach (var  source in secondaryFrameNode.UsingParameters
                                                          .ParameterNames)
                {
                    if (orderedParameters.Any(k => string.Equals(k, source, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        Debug.Assert(secondaryFrameNode.Query.Conversions.ContainsKey(source), "secondaryFrameNode.Query.Conversions.ContainsKey(source), source:"+source);
                        plan.AddConversion(source, secondaryFrameNode.Query.Conversions[source]);
                    }
                }
               
            }
            nodesWithQueries = FillPriorityGroupAndCleanDuplicates(nodesWithQueries);
            var factory = new AccumulatorFactory();
            foreach (var secondaryFrameNode in nodesWithQueries)
            {
                MakePlanStepFromSecondary(plan, factory,secondaryFrameNode);
            }          
        }

        public FramingPlan MakePlan(ProjectionElement root, int depth,ParameterNames pars)
        {           
            var plan = new FramingPlan(root,_settingsHolder.Settings.CheckMode);           
            CheckNodeVariants(plan, root , depth, pars);
            return plan;
        }
    }
}
