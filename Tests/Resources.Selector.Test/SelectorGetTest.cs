using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Resources.Selector.Test
{

    [TestClass]
    public sealed class SelectorGetTest
    {

        private CompositionContainer _container;

      

        public TestContext Context { get; set; }

        
        private void Initialize(params Enum[] flags)
        {
            var setting = new Settings {ActivationFlags = flags};
            _container.GetExportedValue<ICacheServicing>().Initialize(setting, _container);
        }

        private void CheckResource(params string[] desiredNames)
        {
            var test = _container.GetExportedValue<ITypedPool>().Get<TestResource>(Guid.Empty);
            Assert.IsTrue(desiredNames.Contains(test.GetStyle));
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void SimpleTest()
        {
            Initialize();
            CheckResource("X1","X2","X1Y1","X2Y1");//в случае двусмысленного выбора геттер не определен
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X1Test()
        {            
            Initialize(SwitchX.X1);
            CheckResource("X1",  "X1Y1");
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X2Test()
        {
            Initialize(SwitchX.X2);
            CheckResource("X2", "X2Y1");
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void Y1Test()
        {
            Initialize(SwitchY.Y1);
            CheckResource("X1", "X2", "X1Y1", "X2Y1");
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void Y2Test()
        {
            Initialize(SwitchY.Y2);
            CheckResource("X1", "X2");               
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X1Y1Test()
        {
            Initialize(SwitchX.X1,SwitchY.Y1);
            CheckResource("X1", "X1Y1");
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X1Y2Test()
        {
            Initialize(SwitchX.X1, SwitchY.Y2);
            CheckResource("X1");
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X2Y1Test()
        {
            Initialize(SwitchX.X2, SwitchY.Y1);
            CheckResource("X2", "X2Y1");
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X2Y2Test()
        {
            Initialize(SwitchX.X2, SwitchY.Y2);
            CheckResource("X2");
        }

        [TestInitialize]
        public void Prepare()
        {
            var catalog = new AggregateCatalog(
                new AssemblyCatalog(typeof (ITypedPool).Assembly),
                new AssemblyCatalog(typeof (TestResource).Assembly));
            _container = new CompositionContainer(catalog);
        }
    }
}
