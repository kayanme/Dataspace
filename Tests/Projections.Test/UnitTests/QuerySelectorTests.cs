using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Projections;
using Dataspace.Common.Projections.Classes;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.Projections.Services.PlanBuilding;

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
                                                     new ResourceQuerier.FuncWithSortedArgs(new[] {"Parent"},
                                                                                                     (string[] s) =>  new[] {id},
                                                                                                     (object[] s) =>  new[] {id},
                                                                                                     typeof (object),
                                                                                                     new Func<string, object>[]{ k2=>k2})
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
            var parent = new ProjectionElement { Name = "Parent" };
            var relation = new Relation
            {
                ParentElement = parent,
                Queries = new[]
                                                 {
                                                     new ResourceQuerier.FuncWithSortedArgs(new[] {"Parent"},
                                                                                                     (string[] s) =>  new[] {id},
                                                                                                     (object[] s) =>  new[] {id},
                                                                                                     typeof (object),
                                                                                                     new Func<string, object>[]{ k2=>k2})
                                                 },
                QueriesFromPhysicalSpace = new[]
                                                 {
                                                     new ResourceQuerier.FuncWithSortedArgs(new[] {"Parent"},
                                                                                                     (string[] s) => new[] {id2},
                                                                                                     (object[] s) => new[] {id2},
                                                                                                     typeof (object),
                                                                                                     new Func<string, object>[]{ k2=>k2})
                                                 }
            };
            ParameterNames names;
            var q = _tester.FormGetter(relation, new BoundingParameter[0], out names);
            var k = Guid.NewGuid();
            Assert.AreEqual(k, q(new[] { k }, new Dictionary<string, object>()).First().Key);
            Assert.AreEqual(id, q(new[] { k }, new Dictionary<string, object>()).First().Value.First());
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
            var parent = new ProjectionElement { Name = "Parent" };
            var relation = new Relation
            {
                ParentElement = parent,
                Queries = new[]
                                                 {
                                                     new ResourceQuerier.FuncWithSortedArgs(new[] {"Parent"},
                                                                                                     (string[] s) =>new[] {id},
                                                                                                     (object[] s) =>new[] {id},
                                                                                                     typeof (object),
                                                                                                     new Func<string, object>[]{ k2=>k2})
                                                 },
                QueriesFromPhysicalSpace = new[]
                                                 {
                                                     new ResourceQuerier.FuncWithSortedArgs(new[] {"Parent","Option2","Option"},
                                                                                                     (s) => s.Select(k2=>new Guid(k2)).ToArray(),
                                                                                                     (s) => s.OfType<Guid>().ToArray(),
                                                                                                     typeof (object),
                                                                                                     new Func<string, object>[]{ k2=>new Guid(k2), k2=>new Guid(k2), k2=>new Guid(k2)})
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
                                                                      {"Option", id}});

            Assert.AreEqual(k,result.First().Key);
            Assert.AreEqual(k, result.First().Value.First());
            Assert.AreEqual(id2, result.First().Value.Skip(1).First());
            Assert.AreEqual(id, result.First().Value.Skip(2).First());
            Assert.IsTrue(names.SequenceEqual(new []{"Option","Option2"}));
            
        }
    }
}
