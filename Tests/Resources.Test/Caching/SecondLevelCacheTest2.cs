using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dataspace.Common.Statistics;
using Dataspace.Common.Utility.Dictionary;
using Common.Utility.Dictionary;
using Dataspace.Common.Utility.Dictionary.SecondLevelCache;
using Testhelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Resources.Test.Caching
{
   
    [TestClass]
    public class SecondLevelCacheTest2
    {
       


        private class TestElement
        {
            public int Value;
            public float Frequency;
        }

        private bool _wasRebalancing;

        private SecondLevelCache<Guid, TestElement> CreateCache()
        {
            var cache = new SecondLevelCache<Guid, TestElement>(Comparer<Guid>.Default,
                                                                k => k.Frequency,
                                                                a =>
                                                                    {
                                                                        a();
                                                                        _wasRebalancing = true;
                                                                    });
            return cache;
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void SimpleCacheTest()
        {
            var key = Guid.NewGuid();
            var element = new TestElement {Frequency = 1, Value = 1};
            var cache = CreateCache();
            cache.Add(key,element);
            var foundElement = cache.Find(key);
            Assert.AreEqual(element.Value,foundElement.Value);
            Assert.IsFalse(_wasRebalancing);
        }


        [TestMethod]
        [TestCategory("Caching")]
        public void CacheTestWithNoRebalancing()
        {
            var key = Guid.NewGuid();
            var element = new TestElement { Frequency = 1, Value = 1 };
            var cache = CreateCache();
             cache.Add(key, element);
            for (var i = 0; i < cache.State.AdaptationSettings.CheckThreshold + 1;i++)
                cache.Find(key);
            var foundElement = cache.Find(key);
            Assert.AreEqual(element.Value, foundElement.Value);
            Assert.IsFalse(_wasRebalancing);//не будет ребалансировки, потому что ожидаемый путь (1 изначально) совпадает с фактическим
        }
                 
        [TestMethod]
        [TestCategory("Caching")]
        public void MultipleCacheTestWithRebalancingAndCheck()
        {
          
            const int count = 100;

            var keys = MockHelper.GetRandomSequence<Guid>(count).ToArray();
            var elements = MockHelper.GetRandomSequence<float>(count).Select(k => new TestElement { Frequency = k, Value = (int)k }).ToArray();
            var cache = CreateCache();

            for (int i = 0; i < count;i++ )
            {
                cache.Add(keys[i],elements[i]);
            }
            var rnd = new Random();
            for (var i = 0; i < cache.State.AdaptationSettings.CheckThreshold + 1; i++)
            {
                var seed = rnd.NextDouble();
                for (int j = 0; j < count; j++)
                {
                    if (elements[j].Frequency > seed)
                    {
                        cache.Find(keys[j]);
                    }
                }
            }           

            Assert.AreEqual(cache.State.ExpectedRate, cache.State.Rate, 2);
            Assert.IsTrue(_wasRebalancing);
        }


     

     
    }
}
