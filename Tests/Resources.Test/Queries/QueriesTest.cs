using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.ServiceResources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resources.Test.TestResources;

namespace Resources.Test
{
    [TestClass]
    public class QueriesTest
    {

        internal IEnumerable<Guid> NoArgQuery()
        {
            return new[]{Guid.Empty};
        }

        internal IEnumerable<Guid> SimpleQuery(string name)
        {
            return new[] { Guid.Empty };
        }

        private IEnumerable<Guid> SimpleQueryForResource(Guid resource)
        {
            return new[] { Guid.Empty };
        }

        private IEnumerable<Guid> TwoArgSimpleQuery(string name,string type)
        {
            if (name == "Name" && type == "Type")
                return new[] { Guid.Empty };
            return new[] { Guid.NewGuid() };
        }

        private IEnumerable<string> BadSimpleQuery(string name)
        {
            return new[] { "" };
        }

        private IEnumerable<KeyValuePair<Guid,IEnumerable<Guid>>> SeriaQuery(IEnumerable<Guid> resource)
        {
            return new[] { new KeyValuePair<Guid, IEnumerable<Guid>>(Guid.Empty,new[]{Guid.Empty}) };
        }

        private IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>> SeriaQueryWithArg(string name, IEnumerable<Guid> resource)
        {
            return new[] { new KeyValuePair<Guid, IEnumerable<Guid>>(Guid.Empty, new[] { Guid.Empty }) };
        }

        private IEnumerable<Guid> BadSeriaQuery(string name, IEnumerable<Guid> resource)
        {
            return new[] { Guid.Empty  };
        }
            
        private Query GetQueryFor(string name)
        {
            
           return Query.CreateFromMethod(Guid.Empty, "", this,
               typeof(QueriesTest).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance));
        }

        [TestMethod]
        [TestCategory("Queries")]
        public void SimpleQueryInfoFromMethod()
        {
            var q = GetQueryFor("NoArgQuery");
            Assert.AreEqual(0,q.ArgCount);
            Assert.AreEqual(new ParameterNames(), q.Arguments);
            Assert.AreEqual(Guid.Empty, q.ResourceKey);            
        }

        [TestMethod]
        [TestCategory("Queries")]
        public void ExecuteQueryInfoFromSimpleMethod()
        {
            var q = GetQueryFor("SimpleQuery");
            Assert.AreEqual(new ParameterNames("name"),q.Arguments);
            Assert.IsFalse(q.SerialQueryIsPreferred("name"));
        }

        [TestMethod]
        [TestCategory("Queries")]
        public void ExecuteQueryInfoFromSeriaWithArgMethod()
        {
            var q = GetQueryFor("SeriaQueryWithArg");
            Assert.AreEqual(new ParameterNames("name","resource"), q.Arguments);
            Assert.IsFalse(q.SerialQueryIsPreferred("name"));
            Assert.IsTrue(q.SerialQueryIsPreferred("resource"));
        }


        [TestMethod]
        [TestCategory("Queries")]
        public void ExecuteSimpleMethod()
        {
            var q = GetQueryFor("SimpleQuery");
            var method = q.GetQueryMethod("name");
            Assert.AreEqual(Guid.Empty, method("!").Single());
        }

        [TestMethod]
        [TestCategory("Queries")]
        public void ExecuteTwoArgSimpleMethod()
        {
            var q = GetQueryFor("SimpleQuery");
            var method = q.GetQueryMethod("type","name");
            Assert.AreEqual(Guid.Empty, method("Type", "Name").Single());
        }

        [TestMethod]
        [TestCategory("Queries")]
        public void ExecuteSeriaMethodAsSimple()
        {
            var q = GetQueryFor("SeriaQuery");
            var method = q.GetQueryMethod("resource");
            Assert.AreEqual(Guid.Empty, method(new[]{Guid.Empty}).Single());
        }

        [TestMethod]
        [TestCategory("Queries")]
        public void ExecuteSeriaMethodWithParamsAsSimple()
        {
            var q = GetQueryFor("SeriaQueryWithArg");
            var method = q.GetQueryMethod("name","resource");
            Assert.AreEqual(Guid.Empty, method("Name",new[] { Guid.Empty }).Single());
        }

        [TestMethod]
        [TestCategory("Queries")]
        public void ExecuteResourceMethodAsSimpleForParent()
        {
            var q = GetQueryFor("SimpleQueryForResource");
            var method = q.GetSingleChildResourceQuery("resource");
            Assert.AreEqual(Guid.Empty, method(Guid.Empty).Single());
        }

        [TestMethod]
        [TestCategory("Queries")]
        public void ExecuteResourceMethodAsSeriaForParent()
        {
            var q = GetQueryFor("SimpleQueryForResource");
            var method = q.GetMultipleChildResourceQuery("resource");
            Assert.AreEqual(Guid.Empty, method(new[]{Guid.Empty}).Single().Key);
            Assert.AreEqual(Guid.Empty, method(new[] { Guid.Empty }).Single().Value.Single());
        }

        [TestMethod]
        [TestCategory("Queries")]
        public void ExecuteSeriaResourceMethodAsSimpleForParent()
        {
            var q = GetQueryFor("SeriaQuery");
            var method = q.GetSingleChildResourceQuery("resource");
            Assert.AreEqual(Guid.Empty, method(Guid.Empty).Single());
        }

        [TestMethod]
        [TestCategory("Queries")]
        public void ExecuteSeriaResourceMethodAsSeriaForParent()
        {
            var q = GetQueryFor("SeriaQuery");
            var method = q.GetMultipleChildResourceQuery("resource");
            Assert.AreEqual(Guid.Empty, method(new[] { Guid.Empty }).Single().Key);
            Assert.AreEqual(Guid.Empty, method(new[] { Guid.Empty }).Single().Value.Single());
        }

        [TestMethod]
        [TestCategory("Queries")]
        public void ExecuteSeriaResourceWithArgMethodAsSimpleForParent()
        {
            var q = GetQueryFor("SeriaQueryWithArg");
            var method = q.GetSingleChildResourceQuery("Resource","Name");
            Assert.AreEqual(Guid.Empty, method(Guid.Empty,"Name").Single());
        }

        [TestMethod]
        [TestCategory("Queries")]
        public void ExecuteSeriaResourceWithArgMethodAsSeriaForParent()
        {
            var q = GetQueryFor("SeriaQueryWithArg");
            var method = q.GetMultipleChildResourceQuery("Resource","Name");
            Assert.AreEqual(Guid.Empty, method(new[] { Guid.Empty }, "Name").Single().Key);
            Assert.AreEqual(Guid.Empty, method(new[] { Guid.Empty }, "Name").Single().Value.Single());
        }

        [TestMethod]
        [TestCategory("Queries")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetErrorFromBadSeriaMethod()
        {
            var q = GetQueryFor("BadSeriaQuery");            
            q.GetMultipleChildResourceQuery("resource");           
        }
    }
}
