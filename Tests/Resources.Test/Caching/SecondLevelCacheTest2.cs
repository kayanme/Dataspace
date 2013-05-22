using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dataspace.Common.Statistics;
using Dataspace.Common.Utility.Dictionary;
using Common.Utility.Dictionary;
using Testhelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Resources.Test.Caching
{
    /// <summary>
    /// Для всех тестов, считающих расхождение между ожидаемым временем получения и реальным проверяется достаточно большая вилка в связи
    ///  с высокодисперсным нормальным распределением (а надо пуассоновское).
    /// </summary>
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
            var cache = new SecondLevelCache<Guid, TestElement>(Comparer<Guid>.Default,  k => k.Frequency, a =>
                                                                                                                  {
                                                                                                                      a();
                                                                                                                      _wasRebalancing  = true;                                                                                                                     
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
            for (var i = 0; i < cache.State.CheckThreshold + 1;i++)
                cache.Find(key);
            var foundElement = cache.Find(key);
            Assert.AreEqual(element.Value, foundElement.Value);
            Assert.IsFalse(_wasRebalancing);//не будет ребалансировки, потому что ожидаемый путь (1 изначально) совпадает с фактическим
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void CacheTestWithRebalancing()
        {
            var key = Guid.NewGuid();
            var key2 = Guid.NewGuid();
            var element = new TestElement { Frequency = 0.5F, Value = 1 };
            var element2 = new TestElement { Frequency = 0.5F, Value = 1 };
            var cache = CreateCache();
            cache.Add(key, element);
            cache.Add(key2, element2);
            for (var i = 0; i < cache.State.CheckThreshold + 1; i++)
                cache.Find(key2);
            var foundElement = cache.Find(key);
            Assert.AreEqual(element.Value, foundElement.Value);
            Assert.IsTrue(_wasRebalancing);
            Assert.AreEqual(1.5,cache.State.ExpectedRate);
            Assert.AreEqual(1, cache.State.CurrentPath);            
        }


        [TestMethod]
        [TestCategory("Caching")]
        public void CacheTestWithRebalancingAndCheck()
        {
            var key = Guid.NewGuid();
            var key2 = Guid.NewGuid();
            var element = new TestElement { Frequency = 0.5F, Value = 1 };
            var element2 = new TestElement { Frequency = 0.5F, Value = 1 };
            var cache = CreateCache();
            cache.Add(key, element);
            cache.Add(key2, element2);
            for (var i = 0; i < cache.State.CheckThreshold + 1; i++)
                cache.Find(key);

            for (var i = 0; i < cache.State.CheckThreshold + 1; i++)
            {
                cache.Find(key);
                cache.Find(key2);
            }

            Assert.AreEqual(cache.State.ExpectedRate, cache.State.Rate, 0.1); 
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
            for (var i = 0; i < cache.State.CheckThreshold + 1; i++)
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


        [TestMethod]
        [TestCategory("Caching")]
        public void MultipleFullCacheTestWithRebalancingAndCheck()
        {

            const int count = 100;

            var keys = MockHelper.GetRandomSequence<Guid>(count).ToArray();
            var elements = MockHelper.GetRandomSequence<float>(count).Select(k => new TestElement { Frequency = k, Value = (int)k }).ToArray();
            var cache = new UpgradedCache<Guid, TestElement, UpdatableElement<TestElement>>(queueRebalance:a=>a());
            var mock = new UpgradedCache<Guid, TestElement, UpdatableElement<TestElement>>.TestMock(cache);
            mock.SecondLevel.State.MaxFixedBranchDepth = 4;
            for (int i = 0; i < count; i++)
            {
                cache.Push(keys[i], elements[i]);
            }
            var rnd = new Random();
          
            for (var i = 0; i < mock.SecondLevel.State.CheckThreshold + 1; i++)
            {
                Thread.Sleep(1);
                var seed = rnd.NextDouble();
                for (int j = 0; j < count; j++)
                {
                    if (elements[j].Frequency > seed)
                    {
                        cache.Push(keys[j], elements[j]);
                    }
                }
            }

            Assert.AreEqual(mock.SecondLevel.State.ExpectedRate, mock.SecondLevel.State.Rate, 2);           
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void MultipleFullCacheTestWithRebalancingAndGarbageCollecting()
        {

            const int count = 100;

            var keys = MockHelper.GetRandomSequence<Guid>(count).ToArray();
            var elements = MockHelper.GetRandomSequence<float>(count).Select((k, i) => new TestElement { Frequency = k, Value = i }).ToArray();
            var cache = new UpgradedCache<Guid, TestElement, UpdatableElement<TestElement>>(queueRebalance: a => a());
            var mock = new UpgradedCache<Guid, TestElement, UpdatableElement<TestElement>>.TestMock(cache);
            mock.SecondLevel.State.MaxFixedBranchDepth = 4;
            for (int i = 0; i < count; i++)
            {
                cache.Push(keys[i], elements[i]);
            }
            var rnd = new Random();
         
            for (var i = 0; i < mock.SecondLevel.State.CheckThreshold / 10 + 1; i++)
            {
                Thread.Sleep(1);
                var seed = rnd.NextDouble();
                for (int j = 0; j < count; j++)
                {
                    if (elements[j].Frequency > seed)
                    {
                        cache.Push(keys[j], elements[j]);
                    }
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.AreEqual(mock.SecondLevel.Count,mock.SecondLevel.Keys.Count());
            var minKeyIndex = elements.OrderBy(k => k.Frequency).First().Value;
            var minKey = keys[minKeyIndex];
            var minElement = cache.RetrieveByFunc(minKey, k=>null);//после сборки мусора элемент с минимальной частотой точно должен умереть, скорее всего кто-то еще, но это неважно.
            Assert.IsNull(minElement);
        }
    }
}
