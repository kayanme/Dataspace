using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Projections.Classes;
using Dataspace.Common.Projections.Services.PlanBuilding;
using Dataspace.Common.ServiceResources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Projections.Test.UnitTests
{
    [TestClass]
    public class RelationTests
    {
        [TestMethod]
        [TestCategory("Projections")]
        public void QuerySelectionBetweenMultipleAndNot()
        {
            var parent = new ProjectionElement {Name = "parent"};
            var child = new ProjectionElement {Name = "child"};
            var query1 = Query.CreateTestStubQuery("", null, "parent");
            var query2 = Query.CreateTestStubMultipleQuery(parent.Name);
            var relation = new Relation
                               {
                                   ParentElement = parent,
                                   ChildElement = child,
                                   Queries = new[]
                                                 {
                                                     query1, query2
                                                 }
                               };
            var bestQuery = relation.SelectTheBestQuery(new BoundingParameter[0]).First();
            Assert.AreSame(query2 ,bestQuery);
        }

        [TestMethod]
        [TestCategory("Projections")]
        public void QuerySelectionWithParamsAndNot()
        {
            var parent = new ProjectionElement {Name = "parent"};
            var child = new ProjectionElement {Name = "child"};
            var query1 = Query.CreateTestStubQuery("", null, "parent", "name");
            var query2 = Query.CreateTestStubMultipleQuery(parent.Name);
            var relation = new Relation
                               {
                                   ParentElement = parent,
                                   ChildElement = child,
                                   Queries = new[]
                                                 {
                                                     query1, query2
                                                 }
                               };
            var bestQuery = relation.SelectTheBestQuery(new[] {new BoundingParameter("name", 1)}).First();
            Assert.AreSame(query1, bestQuery);
        }

        [TestMethod]
        [TestCategory("Projections")]
        public void QuerySelectionWithParamsAndNot2()
        {
            var parent = new ProjectionElement { Name = "parent" };
            var child = new ProjectionElement { Name = "child" };
            var query1 = Query.CreateTestStubQuery("", null, "parent");
            var query2 = Query.CreateTestStubMultipleQuery(parent.Name);
            var relation = new Relation
            {
                ParentElement = parent,
                ChildElement = child,
                Queries = new[]
                                                 {
                                                     query1, query2
                                                 }
            };
            var bestQuery = relation.SelectTheBestQuery(new[] { new BoundingParameter("name", 1) }).First();
            Assert.AreSame(query2, bestQuery);
        }

        [TestMethod]
        [TestCategory("Projections")]
        public void QuerySelectionWithParamsAtDifferentDepths()
        {
            var parent = new ProjectionElement { Name = "parent" };
            var child = new ProjectionElement { Name = "child" };
            var query1 = Query.CreateTestStubQuery("", null, "parent","closer");
            var query2 = Query.CreateTestStubQuery("", null, "parent","further");
            var relation = new Relation
                               {
                                   ParentElement = parent,
                                   ChildElement = child,
                                   Queries = new[]
                                                 {
                                                     query1, query2
                                                 }
                               };
            var bestQuery = relation.SelectTheBestQuery(new[] { new BoundingParameter("closer", 1), new BoundingParameter("further", 2) }).First();
            Assert.AreSame(query1, bestQuery);
        }
    }
}
