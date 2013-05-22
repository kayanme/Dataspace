using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
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
    public sealed class SelectorAppConfigGetTest
    {

        private CompositionContainer _container;

        public TestContext Context { get; set; }

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
            CheckResource("X1", "X2", "X1Y1", "X2Y1"); //в случае двусмысленного выбора геттер не определен

        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X1Test()
        {
            Initialize("X:X1");
            CheckResource("X1", "X1Y1");

        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X2Test()
        {

            Initialize("X:X2");
            CheckResource("X2", "X2Y1");

        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void Y1Test()
        {

            Initialize("Y:Y1");
            CheckResource("X1", "X2", "X1Y1", "X2Y1");

        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void Y2Test()
        {

            Initialize("Y:Y2");
            CheckResource("X1", "X2");

        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X1Y1Test()
        {

            Initialize("X:X1", "Y:Y1");
            CheckResource("X1", "X1Y1");

        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X1Y2Test()
        {

            Initialize("X:X1", "Y:Y2");
            CheckResource("X1");

        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X2Y1Test()
        {

            Initialize("X:X2", "Y:Y1");
            CheckResource("X2", "X2Y1");

        }

        [TestMethod]
        [TestCategory("Selectors")]
        public void X2Y2Test()
        {

            Initialize("X:X2", "Y:Y2");
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
