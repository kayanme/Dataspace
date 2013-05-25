using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Projections.Classes;
using Dataspace.Common.Projections.Classes.Descriptions;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.Projections.Services;
using Dataspace.Common.ServiceResources;
using Dataspace.Common.Utility;
using Projections.Test.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Attribute = System.Attribute;

namespace Projections.Test.UnitTests
{
    [TestClass]
    public sealed class CommendationCollectionTest
    {

        [TestMethod]
        [TestCategory("Projections")]
        public void RecursiveCommendationTest()
        {
           

            var sElement = new ResourceProjectionElement
                               {
                                   Name = "Attribute",
                                   FillingInfo = new FillingInfo {{"Name", FillingInfo.FillType.Native}},
                                   ResourceType = "Attribute",
                                   Namespace = "Test"
                               };
            var sGroup = new ResourceProjectionElement
                             {
                                 Name = "Element",
                                 FillingInfo = new FillingInfo {{"Name", FillingInfo.FillType.Native}},
                                 ResourceType = "Element",
                                 Namespace = "Test",
                             };

            var gToGRelation = new Relation {ChildElement = sGroup, ParentElement = sGroup};
            var eFromEQuery = Query.CreateTestStubQuery(null,null,"element");
            gToGRelation.Queries = new[] {eFromEQuery};
            sGroup.UpRelations.Add(gToGRelation);
            sGroup.DownRelations.Add(gToGRelation);

            var eToGRelation = new Relation {ChildElement = sElement, ParentElement = sGroup};
            var aFromEQuery = Query.CreateTestStubQuery(null,null,"element");
            eToGRelation.Queries = new[] {aFromEQuery};
            sElement.UpRelations.Add(eToGRelation);
            sGroup.DownRelations.Add(eToGRelation);

            var cc = new CommendationCollection();
           
            var acc = GetAccumulator(gToGRelation, eFromEQuery);
            cc.AddNewStep(new PlanStep(sGroup, sGroup, new ParameterNames(), acc, new ParameterNames(), 0));
            acc = GetAccumulator(eToGRelation, aFromEQuery);
            cc.AddNewStep(new PlanStep(sGroup, sElement, new ParameterNames(), acc, new ParameterNames(), 0));

            var root = new FrameNodeGroup(sGroup,
                                          new[] {new FrameNode(Guid.Empty, sGroup, 0, new Dictionary<string, object>())},
                                          new ParameterNames());
            var nodes = cc.GetNewGroups(new[] {root});
            Assert.AreEqual(2,nodes.Count());
            nodes = cc.GetNewGroups(nodes);
            Assert.AreEqual(2, nodes.Count());
            nodes = cc.GetNewGroups(nodes);
            Assert.AreEqual(2, nodes.Count());
        }

        private Accumulator<FrameNode, IEnumerable<Guid>> GetAccumulator(Relation relation,Query query)
        {
            var factory = new AccumulatorFactory();
            var plan = new FramingPlan(relation.ParentElement);
            var acc = factory.GetOrCreateAccumulator(plan,
                                        (keys, parPairs) =>
                                            query.GetMultipleChildResourceQuery(relation.ParentElement.Name,parPairs.Select(k=>k.Key).ToArray())(keys,parPairs.Select(k=>k.Value).ToArray()),
                                      relation.ParentElement, relation.ChildElement, new ParameterNames());
            return acc;
        }
    }

    
}
