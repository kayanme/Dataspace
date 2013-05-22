using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Resources.Selector.Test
{
    [TestClass]
    public sealed class SelectorQueryTest
    {

        private CompositionContainer _container;

        private class MockAppConfig : AppConfigProvider
        {
            private StringDictionary _dict = new StringDictionary();

            public override bool ContainsKey(string key)
            {
                return _dict.ContainsKey(key);
            }

            public override string GetValue(string key)
            {
                return _dict[key];
            }

            public void Add(string key, string value)
            {
                _dict.Add(key, value);
            }
        }

        private void Initialize(params string[] flags)
        {

            var config = new MockAppConfig();
            _container.GetExportedValue<SettingsHolder>().Provider = config;
            foreach (var keyValuePair in flags.Select(k => k.Split(':')))
            {
                config.Add(keyValuePair[0], keyValuePair[1]);
            }
            _container.GetExportedValue<ICacheServicing>().Initialize(new Settings(), _container);
        }

        private void CheckResource(params int[] count)
        {
            var test = _container.GetExportedValue<ITypedPool>().Get<TestResource>("");
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
            Initialize("X:X1");
            CheckResource(1,3,4);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X2Test()
        {
            Initialize("X:X2");
            CheckResource(2);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void Y1Test()
        {
            Initialize("Y:Y1");
            CheckResource(1,2,3);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void Y2Test()
        {
            Initialize("Y:Y1");
            CheckResource(1,2,4);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X1Y1Test()
        {
            Initialize("X:X1", "Y:Y1");
            CheckResource(1,3);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X1Y2Test()
        {
            Initialize("X:X1", "Y:Y2");
            CheckResource(1,4);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X2Y1Test()
        {
            Initialize("X:X2", "Y:Y1");
            CheckResource(2);
        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X2Y2Test()
        {
            Initialize("X:X2", "Y:Y2");
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
