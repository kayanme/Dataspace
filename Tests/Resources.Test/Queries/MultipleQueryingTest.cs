using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resources.Test.TestResources;
using Testhelper;

namespace Resources.Test.Queries
{
    [TestClass]
    public  class MultipleQueryingTest
    {
        private CompositionContainer _container;
        private ITypedPool _pool;

        protected Assembly[] AssembliesWhichShouldProvideExport
        {
            get { return new[] { typeof(Model).Assembly, typeof(ITypedPool).Assembly }; }
        }

        private Model _model1;
        private Model _model2;
        private Element _e1;
        private Element _e2;
        private Element _e3;

        [TestInitialize]
        public void Preparation()
        {
            _container = MockHelper.InitializeContainer(AssembliesWhichShouldProvideExport, new Type[0]);

            _model1 = new Model { Element = Guid.Empty, Key = Guid.NewGuid(), Name = "M1" };
            _model2 = new Model { Element = Guid.Empty, Key = Guid.NewGuid(), Name = "M2" };
            _e1 = new Element {Key = Guid.NewGuid(), Model = _model1.Key, Name = "El1"};
            _e2 = new Element { Key = Guid.NewGuid(), Model = _model1.Key, Name = "El2" };
            _e3 = new Element { Key = Guid.NewGuid(), Model = _model2.Key, Name = "El3" };
            var pool = new ResourcePool();
            pool.Models.Add(_model1.Key, _model1);
            pool.Models.Add(_model2.Key, _model2);
            pool.Elements.Add(_e1.Key, _e1);
            pool.Elements.Add(_e2.Key, _e2);
            pool.Elements.Add(_e3.Key, _e3);
            _container.ComposeExportedValue(pool);
            _container.ComposeExportedValue(_container);
            Settings.NoCacheGarbageChecking = true;
            _pool = _container.GetExportedValue<ITypedPool>();
        }

        [TestMethod]
        public void MultipleWithoutParameterAndWithoutGrouping()
        {
            object query = _pool.Spec(model: new[] { _model1.Key, _model2.Key });
            var res = _pool.Find<Element>(query).ToArray();
            CollectionAssert.AreEquivalent(res,new[]{_e1.Key,_e2.Key,_e3.Key});
            Assert.IsTrue(ElementQuery.WasCalledMultiple);
        }

        [TestMethod]
        public void MultipleWithoutParameterAndWithGrouping()
        {
            object query = _pool.Spec(model: new[] { _model1.Key, _model2.Key });
            var res = _pool.FindAndGroup<Element>(query,"model").ToArray();
            Assert.AreEqual(2,res.Count());
            CollectionAssert.AreEquivalent(res.First(k=>k.Key == _model1.Key).Value.ToArray(), new[] { _e1.Key, _e2.Key });
            CollectionAssert.AreEquivalent(res.First(k => k.Key == _model2.Key).Value.ToArray(), new[] { _e3.Key });
            Assert.IsTrue(ElementQuery.WasCalledMultiple);
        }

        [TestMethod]
        public void MultipleWithParameterAndWithoutGrouping()
        {
            object query = _pool.Spec(model: new[] { Guid.NewGuid(), Guid.NewGuid() }, modelName: "M1");
            var res = _pool.Find<Element>(query).ToArray();
            CollectionAssert.AreEquivalent( new[] { _e1.Key, _e2.Key},res);
            Assert.IsTrue(ElementQuery.WasCalledMultiple);
        }

        [TestMethod]
        public void MultipleWithParameterAndWithGrouping()
        {
            object query = _pool.Spec(model: new[] { _model1.Key, _model2.Key }, elementName: "El3");
             var res = _pool.FindAndGroup<Element>(query,"model").ToArray();
            Assert.AreEqual(2, res.Count());         
            CollectionAssert.AreEquivalent(new[] { _e3.Key },res.First(k => k.Key == _model2.Key).Value.ToArray());
            CollectionAssert.AreEquivalent(new Guid[0],res.First(k => k.Key != _model2.Key).Value.ToArray());
            Assert.IsTrue(ElementQuery.WasCalledMultiple);
        }

        [TestCleanup]
        public void Shutdown()
        {
            _container.Dispose();
        }
    }
}
