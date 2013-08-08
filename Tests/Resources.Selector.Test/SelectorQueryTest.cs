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
    public sealed class SelectorAppConfigQueryTest
    {

        private CompositionContainer _container;   

        private void Initialize(params Enum[] flags)
        {
            var setting = new Settings { ActivationFlags = flags };
            _container.GetExportedValue<ICacheServicing>().Initialize(setting, _container);
        }

        private void CheckResource(params int[] count)
        {
            var test = _container.GetExportedValue<ITypedPool>().Query<TestResource>("");
            Assert.IsTrue(count.Contains(test.Count()));
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void SimpleTest()
        {
            Initialize();
            CheckResource(1,2,3,4);//в случае двусмысленного выбора геттер не определен
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X1Test()
        {
            Initialize(SwitchX.X1);
            CheckResource(1,3,4);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X2Test()
        {
            Initialize(SwitchX.X2);
            CheckResource(2);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void Y1Test()
        {
            Initialize(SwitchY.Y1);
            CheckResource(1,2,3);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void Y2Test()
        {
            Initialize(SwitchY.Y2);
            CheckResource(1,2,4);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X1Y1Test()
        {
            Initialize(SwitchX.X1, SwitchY.Y1);
            CheckResource(1,3);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X1Y2Test()
        {
            Initialize(SwitchX.X1, SwitchY.Y2);
            CheckResource(1,4);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X2Y1Test()
        {
            Initialize(SwitchX.X2, SwitchY.Y1);
            CheckResource(2);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X2Y2Test()
        {
            Initialize(SwitchX.X2, SwitchY.Y2);
            CheckResource(2);
        }

        [TestInitialize]
        public void Prepare()
        {
            var catalog = new AggregateCatalog(
                new AssemblyCatalog(typeof(ITypedPool).Assembly),
                new AssemblyCatalog(typeof(TestResource).Assembly));
            _container = new CompositionContainer(catalog);
        }
    }
}
