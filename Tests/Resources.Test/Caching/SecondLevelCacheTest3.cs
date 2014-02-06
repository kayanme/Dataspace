using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Utility.Dictionary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Resources.Test.Caching
{
    [TestClass]
    public class SecondLevelCacheTest3
    {

        private class TestElement
        {
            public float Frequency;
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void SimpleCacheTest()
        {
       
            var cache = new SecondLevelCache<int,TestElement>(Comparer<int>.Default,k=>k.Frequency,null,true);
            bool testFailed = false;
            cache.NodeGoneEvent += (o, e) => testFailed = true;
            for(int i=0;i<10000;i++)
            {
                cache.Add(i,new TestElement());
            }
            GC.Collect(2,GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            GC.Collect(2,GCCollectionMode.Forced);
            Assert.IsTrue(!testFailed);
            cache.Dispose();
        }

    }
}
