using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Projections;
using Dataspace.Common.Projections.Classes;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.Projections.Services.PlanBuilding;
using Dataspace.Common.ServiceResources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Projections.Test.UnitTests
{

    [TestClass]
    public sealed class QuerySelectorTests
    {
        private FramingPlanBuilder.TestFramingPlanBuilder _tester = new FramingPlanBuilder.TestFramingPlanBuilder();


        [TestMethod]
        [TestCategory("Projections")]
        public void LoadTest1()
        {
            var id = Guid.NewGuid();
            var parent = new ProjectionElement {Name = "Parent"};
            var relation = new Relation
                               {
                                   ParentElement = parent,
                                   Queries = new[]
                                                 {
                                                     Query.CreateTestStubQuery(null,k2=>new[]{id},"Parent")
                                                 }
                               };
            ParameterNames names;
            var q = _tester.FormGetter(relation, new BoundingParameter[0],out names); 
            var k = Guid.NewGuid();
            Assert.AreEqual(k, q(new[] { k }, new Dictionary<string, object>()).First().Key);
            Assert.AreEqual(id,q(new[] { k },new Dictionary<string, object>()).First().Value.First());
            Assert.IsTrue(names.SequenceEqual(new string[0]));

            q = _tester.FormGetter(relation, new[]{new BoundingParameter("sdsad",0) },out names);
            k = Guid.NewGuid();
            Assert.AreEqual(k, q(new[] { k }, new Dictionary<string, object>()).First().Key);
            Assert.AreEqual(id, q(new[] { k }, new Dictionary<string, object>()).First().Value.First());
            Assert.IsTrue(names.SequenceEqual(new string[0]));
        }


        [TestMethod]
        [TestCategory("Projections")]
        public void ChoiceBetweenPhysicalAndNot()
        {
            var id = Guid.NewGuid();
            var id2= Guid.NewGuid();
            var parent = new ProjectionElement { Name = "Parent",Namespace = "T" };
            var relation = new Relation
                               {
                                   ParentElement = parent,
                                   Queries = new[]
                                                 {
                                                     Query.CreateTestStubQuery("T", k2=>new[]{id},"Parent"),
                                                     Query.CreateTestStubQuery("",k2=>new[]{id2}, "Parent"),
                                                 }

                               };
            ParameterNames names;
            var q = _tester.FormGetter(relation, new BoundingParameter[0], out names);
            var k = Guid.NewGuid();
            var res = q(new[] {k}, new Dictionary<string, object>()).First();
            Assert.AreEqual(k, res.Key);
            Assert.AreEqual(id, res.Value.First());
            Assert.IsTrue(names.SequenceEqual(new string[0]));

            q = _tester.FormGetter(relation, new[] { new BoundingParameter("sdsad", 0) }, out names);
            k = Guid.NewGuid();
            Assert.AreEqual(k, q(new[] { k }, new Dictionary<string, object>()).First().Key);
            Assert.AreEqual(id, q(new[] { k }, new Dictionary<string, object>()).First().Value.First());
            Assert.IsTrue(names.SequenceEqual(new string[0]));
        }


        [TestMethod]
        [TestCategory("Projections")]
        public void ChoiceBetweenPhysicalAndNotWithParameters()
        {
            var id = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var parent = new ProjectionElement { Name = "Parent",Namespace = "T" };
            var relation = new Relation
                               {
                                   ParentElement = parent,
                                   Queries = new[]
                                                 {
                                                     Query.CreateTestStubQuery("T", k2=>new[]{id},"Parent"),                                               
                                                     Query.CreateTestStubQuery("", k2=>k2.OfType<Guid>(),"Parent", "Option2", "Option")
                                                 }
                               };
            ParameterNames names;
            var q = _tester.FormGetter(relation, new BoundingParameter[0], out names);
            var k = Guid.NewGuid();
            Assert.AreEqual(k, q(new[] { k }, new Dictionary<string, object>()).First().Key);
            Assert.AreEqual(id, q(new[] { k }, new Dictionary<string, object>()).First().Value.First());
            Assert.IsTrue(names.SequenceEqual(new string[0]));

            q = _tester.FormGetter(relation,new[] {new BoundingParameter( "Option",0)}, out names);            
            Assert.AreEqual(k, q(new[] { k }, new Dictionary<string, object>()).First().Key);
            Assert.AreEqual(id, q(new[] { k }, new Dictionary<string, object>()).First().Value.First());
            Assert.IsTrue(names.SequenceEqual(new string[0]));
            
            q = _tester.FormGetter(relation, new[] { new BoundingParameter("Option", 2), new BoundingParameter("Option2", 2) }, out names);
            k = Guid.NewGuid();
            var result = q(new[] {k}, new Dictionary<string, object> {{"Option2", id2},
                                                                      {"Option", id}}).First();

            Assert.AreEqual(k,result.Key);
            Assert.AreEqual(id, result.Value.First());
            //Assert.AreEqual(id2, result.Value.Skip(1).First());
            //Assert.AreEqual(id, result.Value.Skip(2).First());
            Assert.IsTrue(names.SequenceEqual(new string[0]));
            
        }
    }
}
