using System;
using System.Collections.Generic;
using System.Linq;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Projections;
using Dataspace.Common.Projections.Classes;
using Dataspace.Common.Projections.Classes.Descriptions;
using Dataspace.Common.Projections.Classes.Plan;

using Projections.Test.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Attribute = Projections.Test.Data.Attribute;

namespace Projections.Test.UnitTests
{

    [TestClass]
    public class PlanBuilderTests
    {

        private FramingPlanBuilder.TestFramingPlanBuilder _tester = new FramingPlanBuilder.TestFramingPlanBuilder();


        [TestMethod]
        [TestCategory("Projections")]
        public void SimplePlan1()
        {
           
            var sAttr = new ResourceProjectionElement
            {
                Name = "Attribute",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Attribute",
                Namespace = "Test"
            };
            var sElement = new ResourceProjectionElement
            {
                Name = "Element",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Element",
                Namespace = "Test",
            };

            var relation = new Relation {ChildElement = sAttr, ParentElement = sElement};
            sAttr.UpRelations.Add(relation);
            sElement.DownRelations.Add(relation);
            var query = MakeStubQuery(typeof(Attribute), "element"); 
            relation.Queries = new[]
                                   {
                                     query
                                   };

            var commendations = _tester.CheckVariantsForNodes(sElement, 2, new ParameterNames());

            Assert.AreEqual(1,commendations.Count());
          
            var elementCommendation = commendations.FirstOrDefault(k => k.MatchedElement == sElement) as TestPlanStep;         
            Assert.IsNotNull(elementCommendation);
          

            Assert.AreEqual(0, elementCommendation.UsedParameters.Count());
            Assert.AreEqual(sAttr, elementCommendation.ProducedChildElement);       
            Assert.AreEqual(query, elementCommendation.Query);
         
        }

        private ResourceQuerier.SeriesFuncWithSortedArgs MakeStubQuery(string parent, Type targetResType, params string[] args)
        {
            return new ResourceQuerier.SeriesFuncWithSortedArgs(parent,
                                                                                  args,
                                                                                  (e, k) => new[] { new KeyValuePair<Guid, IEnumerable<Guid>>(Guid.Empty, new Guid[0]) },
                                                                                  (e, k) => new[] { new KeyValuePair<Guid, IEnumerable<Guid>>(Guid.Empty, new Guid[0]) },
                                                                                  targetResType,
                                                                                  args.Select(a => new Func<string, object>(k => k)).ToArray());
        }

        private ResourceQuerier.FuncWithSortedArgs MakeStubQuery( Type targetResType, params string[] args)
        {
            return new ResourceQuerier.FuncWithSortedArgs(args,
                                                          (string[] e) => new Guid[0],
                                                          (object[] e) => new Guid[0],
                                                         targetResType,
                                                        args.Select(a=>new Func<string, object>(k => k)).ToArray());
        }

        private void TestSerialGetters(ParameterNames parameters,int depth = -1)
        {

            //             Е (Е)
            //         A (A) |
            //              V(V)   
            //            V (E)
            //

            var sAttr = new ResourceProjectionElement
            {
                Name = "Attribute",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Attribute",
                Namespace = "Test"
            };
            var sElement = new ResourceProjectionElement
            {
                Name = "Element",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Element",
                Namespace = "Test",
            };

            var sValue = new ResourceProjectionElement
            {
                Name = "Value",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Value",
                Namespace = "Test",
            };

            var sValue2 = new ResourceProjectionElement
            {
                Name = "Value",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Element",
                Namespace = "Test",
            };

            var attrFromElementQuery = MakeStubQuery("Element", typeof(Data.Attribute));
            var valueFromElementQuery = MakeStubQuery("Element", typeof(Data.Value));
            var valueFromAttrQuery = MakeStubQuery("Attribute", typeof(Data.Value));
            var elementFromValueQuery =  MakeStubQuery("Value",typeof(Element));


            var relation = new Relation { ChildElement = sAttr, ParentElement = sElement };
            sAttr.UpRelations.Add(relation);
            sElement.DownRelations.Add(relation);
          
            relation.SeriaQueries = new[] { attrFromElementQuery };

            relation = new Relation { ChildElement = sValue, ParentElement = sElement };
            sValue.UpRelations.Add(relation);
            sElement.DownRelations.Add(relation);
        
            relation.SeriaQueries = new[] { valueFromElementQuery };

            relation = new Relation { ChildElement = sValue, ParentElement = sAttr };
            sValue.UpRelations.Add(relation);
            sAttr.DownRelations.Add(relation);
          
            relation.SeriaQueries = new[] { valueFromAttrQuery };

            relation = new Relation { ChildElement = sValue2, ParentElement = sValue };
            sValue2.UpRelations.Add(relation);
            sValue.DownRelations.Add(relation);
          
            relation.SeriaQueries = new[] { elementFromValueQuery };

            var commendations = _tester.CheckVariantsForNodes(sElement, depth, parameters);

            Assert.AreEqual(4, commendations.Count());

            var elementCommendations = commendations.Where(k => k.MatchedElement == sElement).OfType<TestPlanStep>().ToArray();
            Assert.AreEqual(2,elementCommendations.Count());
            var aFromE = elementCommendations.FirstOrDefault(k => k.ProducedChildElement == sAttr);
            Assert.IsNotNull(aFromE);
            Assert.IsTrue(!aFromE.UsedParameters.Any());
            Assert.AreEqual(attrFromElementQuery, aFromE.Query);   
            var vFromE = elementCommendations.FirstOrDefault(k => k.ProducedChildElement == sValue);
            Assert.IsNotNull(vFromE);
            Assert.IsTrue(!vFromE.UsedParameters.Any());
            Assert.AreEqual(valueFromElementQuery, vFromE.Query);
               
            var attrCommendation = commendations.SingleOrDefault(k =>  k.MatchedElement == sAttr) as TestPlanStep;
            Assert.IsNotNull(attrCommendation);            
            Assert.IsTrue(!attrCommendation.UsedParameters.Any());
            Assert.AreEqual(valueFromAttrQuery, attrCommendation.Query);
            Assert.AreEqual(sValue, attrCommendation.ProducedChildElement);


            var value1Commendation = commendations.SingleOrDefault(k => k.MatchedElement == sValue) as TestPlanStep;
            Assert.IsNotNull(value1Commendation);
            Assert.IsTrue(!value1Commendation.UsedParameters.Any());
            Assert.AreEqual(elementFromValueQuery, value1Commendation.Query);
            Assert.AreEqual(sValue2, value1Commendation.ProducedChildElement);

  
        }

        [TestMethod]
        [TestCategory("Projections")]
        public void SimplePlanWithSerialGetters()
        {
            TestSerialGetters(new ParameterNames());
        }

        [TestMethod]
        [TestCategory("Projections")]
        public void SimplePlanWithSerialGettersAndDepth()
        {
            TestSerialGetters(new ParameterNames(),2);
        }


        [TestMethod]
        [TestCategory("Projections")]
        public void SimplePlanWithSerialGettersAndParameters()
        {
            TestSerialGetters(new ParameterNames(new[] {"a","b"}));
        }

        [TestMethod]
        [TestCategory("Projections")]
        public void SimplePlanWithSimpleGetters()
        {
            TestSimpleGetters(new ParameterNames());
        }


        [TestMethod]
        [TestCategory("Projections")]
        public void SimplePlanWithSimpleGettersAndParameters()
        {
            TestSimpleGetters(new ParameterNames(new[] { "a", "b" }));
        }

        [TestMethod]
        [TestCategory("Projections")]
        public void RecursivePlanWithSimpleGetters()
        {
            var sElement = new ResourceProjectionElement
            {
                Name = "Attribute",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Attribute",
                Namespace = "Test"
            };
            var sGroup = new ResourceProjectionElement
            {
                Name = "Element",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Element",
                Namespace = "Test",
            };

            var relation = new Relation { ChildElement = sGroup, ParentElement = sGroup };
            var eFromEQuery = MakeStubQuery(typeof (Element), "Element");
            relation.Queries = new[] { eFromEQuery };
            sGroup.UpRelations.Add(relation);
            sGroup.DownRelations.Add(relation);

            relation = new Relation { ChildElement = sElement, ParentElement = sGroup };
            var aFromEQuery = MakeStubQuery(typeof(Attribute), "element");
            relation.Queries = new[] { aFromEQuery };
            sElement.UpRelations.Add(relation);
            sGroup.DownRelations.Add(relation);

            var commendations = _tester.CheckVariantsForNodes(sGroup, -1, new ParameterNames());

            Assert.AreEqual(2, commendations.Count());

            var elementCommendations = commendations.Where(k => k.MatchedElement == sGroup).OfType<TestPlanStep>().ToArray();
            Assert.AreEqual(2, elementCommendations.Count());
            var eFromE = elementCommendations.FirstOrDefault(k => k.ProducedChildElement == sGroup);
            Assert.IsNotNull(eFromE);
            Assert.IsTrue(!eFromE.UsedParameters.Any());
            Assert.AreEqual(eFromEQuery, eFromE.Query);
            var aFromE = elementCommendations.FirstOrDefault(k => k.ProducedChildElement == sElement);
            Assert.IsNotNull(aFromE);
            Assert.IsTrue(!aFromE.UsedParameters.Any());
            Assert.AreEqual(aFromEQuery, aFromE.Query);

        }

        private void TestSimpleGetters(ParameterNames parameters,int depth = -1)
        {

            var sAttr = new ResourceProjectionElement
            {
                Name = "Attribute",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Attribute",
                Namespace = "Test"
            };
            var sElement = new ResourceProjectionElement
            {
                Name = "Element",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Element",
                Namespace = "Test",
            };

            var sValue = new ResourceProjectionElement
            {
                Name = "Value",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Value",
                Namespace = "Test",
            };

            var sValue2 = new ResourceProjectionElement
            {
                Name = "Value",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Element",
                Namespace = "Test",
            };

            var relation = new Relation { ChildElement = sAttr, ParentElement = sElement };
            sAttr.UpRelations.Add(relation);
            sElement.DownRelations.Add(relation);
            var attrFromElementQuery = MakeStubQuery(typeof (Attribute), "element");
            var valueFromAttrQuery = MakeStubQuery(typeof(Attribute), "attribute");
            var valueFromElementQuery = MakeStubQuery(typeof(Value), "element");
            var elementFromValueQuery = MakeStubQuery(typeof(Element), "value");

            relation.Queries = new[] { attrFromElementQuery  };

            relation = new Relation { ChildElement = sValue, ParentElement = sElement };
            sValue.UpRelations.Add(relation);
            sElement.DownRelations.Add(relation);
            relation.Queries = new[]  {    valueFromElementQuery };

            relation = new Relation { ChildElement = sValue, ParentElement = sAttr };
            sValue.UpRelations.Add(relation);
            sAttr.DownRelations.Add(relation);
            relation.Queries = new[] {   valueFromAttrQuery  };

            relation = new Relation { ChildElement = sValue2, ParentElement = sValue };
            sValue2.UpRelations.Add(relation);
            sValue.DownRelations.Add(relation);
            relation.Queries = new[]    {   elementFromValueQuery  };


            var commendations = _tester.CheckVariantsForNodes(sElement, depth, parameters);

            Assert.AreEqual(4, commendations.Count());

            var elementCommendations = commendations.Where(k => k.MatchedElement == sElement).OfType<TestPlanStep>().ToArray();
            Assert.AreEqual(2, elementCommendations.Count());
            var aFromE = elementCommendations.FirstOrDefault(k => k.ProducedChildElement == sAttr);
            Assert.IsNotNull(aFromE);
            Assert.IsTrue(!aFromE.UsedParameters.Any());
            Assert.AreEqual(attrFromElementQuery, aFromE.Query);
            var vFromE = elementCommendations.FirstOrDefault(k => k.ProducedChildElement == sValue);
            Assert.IsNotNull(vFromE);
            Assert.IsTrue(!vFromE.UsedParameters.Any());
            Assert.AreEqual(valueFromElementQuery, vFromE.Query);

            var attrCommendation = commendations.SingleOrDefault(k => k.MatchedElement == sAttr) as TestPlanStep;
            Assert.IsNotNull(attrCommendation);
            Assert.IsTrue(!attrCommendation.UsedParameters.Any());
            Assert.AreEqual(valueFromAttrQuery, attrCommendation.Query);
            Assert.AreEqual(sValue, attrCommendation.ProducedChildElement);


            var value1Commendation = commendations.SingleOrDefault(k => k.MatchedElement == sValue) as TestPlanStep;
            Assert.IsNotNull(value1Commendation);
            Assert.IsTrue(!value1Commendation.UsedParameters.Any());
            Assert.AreEqual(elementFromValueQuery, value1Commendation.Query);
            Assert.AreEqual(sValue2, value1Commendation.ProducedChildElement);
            
        }

    }
}
