using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dataspace.Common;
using Dataspace.Common.Utility.Dictionary;

using Common.Utility;
using Common.Utility.Dictionary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities = Dataspace.Common.Utilities;
using GuidCache = Dataspace.Common.Utility.Dictionary.UpgradedCache<System.Guid, System.Guid, Common.Utility.Dictionary.UpdatableElement<System.Guid>>;
using TestElementCache = Dataspace.Common.Utility.Dictionary.UpgradedCache<System.Guid, Resources.Test.CachingDictionaryTest.TestElement, Common.Utility.Dictionary.UpdatableElement<Resources.Test.CachingDictionaryTest.TestElement>>;

namespace Resources.Test
{
    [TestClass]        
    public class CachingDictionaryTest
    {
  
        [Serializable]
        internal class TestElement
        {
            public Guid Value;
            public int Updated=0;
        }



        private Func<Guid,TestElement> GetElement(int update = 0)
        {
            return key=>new TestElement {Value = key,Updated = update};
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void BaseCachingDictionaryTests()
        {
            var dictionary = new TestElementCache(queueRebalance: a => Task.Factory.StartNew(a));
            var key = Guid.NewGuid();
            var element = dictionary.RetrieveByFunc(key, GetElement());
            Assert.AreEqual(element.Value,key);
            Assert.AreEqual(element.Updated, 0);

            element = dictionary.RetrieveByFunc(key, GetElement());
            Assert.AreEqual(element.Value, key);
            Assert.AreEqual(element.Updated, 0);

            dictionary.Push(key, new TestElement {Value = key, Updated = 1});
            element = dictionary.RetrieveByFunc(key, GetElement());
            Assert.AreEqual(element.Value, key);
            Assert.AreEqual(element.Updated, 0);
        }




        private void TestKey(TestElementCache dict, Guid key,object lck)
        {

          
            lock (lck)
            {
                var element = dict.RetrieveByFunc(key, GetElement());
                int upd = element.Updated;
                dict.SetUpdateNecessity(key);
                element = dict.RetrieveByFunc(key, GetElement(upd + 1));
                Assert.AreEqual(key, element.Value);
                Assert.AreEqual(upd + 1, element.Updated);
            }
          
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void ParallelCachingDictionaryTests()
        {
            var dictionary = new TestElementCache(queueRebalance: a => Task.Factory.StartNew(a));
            var keys = 10000.Times().Select(k => Guid.NewGuid()).ToArray();
            var locks = keys.ToDictionary(k => k, k => new object());
            keys.Concat(keys)
                .AsParallel()
                .WithDegreeOfParallelism(20)
                .ForAll(key => TestKey(dictionary, key,locks[key]));


        }


        [TestMethod]
        [TestCategory("Caching")]
        public void ParallelCachingWithRandomFlushDictionaryTests()
        {
            var rnd = new Random();
            var dictionary = new TestElementCache(queueRebalance: a => Task.Factory.StartNew(a));
            var keys = 10000.Times().Select(k =>rnd.Next(10000) == 1?Guid.NewGuid():Guid.Empty).ToArray();
            var locks = keys.Where(k=>k!=Guid.Empty).ToDictionary(k => k, k => new object());
            keys.Concat(keys)                
                .AsParallel()
                .WithDegreeOfParallelism(20)
                .ForAll(key =>
                            {
                                if (key == Guid.Empty)
                                    GC.Collect();
                                else
                                    TestKey(dictionary, key,locks[key]);
                            });
        }


        [TestMethod]
        [TestCategory("Caching")]
        public void CacheMovingSimple()
        {
            var dictionary = new TestElementCache(queueRebalance: a => Task.Factory.StartNew(a));
            var test = new TestElementCache.TestMock(dictionary);


            var key = Guid.NewGuid();
            var val = new TestElement {Updated = 1, Value = key};
            dictionary.Push(key, val);
            UpdatableElement<TestElement> fl;
            var res = test.FirstLevel.TryGetValue(key, out fl);
            Assert.IsTrue(res);
            Assert.AreEqual(key,fl.Element.Value);
            Assert.AreEqual(0,test.SecondLevel.Count);
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void CacheMovingWithCollecting()
        {
            var dictionary = new TestElementCache(queueRebalance: a => Task.Factory.StartNew(a));
            var test = new TestElementCache.TestMock(dictionary);


            var key = Guid.NewGuid();
            var val = new TestElement { Updated = 1, Value = key };
            dictionary.Push(key, val);
            UpdatableElement<TestElement> fl;
            GC.Collect();
            var res = test.FirstLevel.TryGetValue(key, out fl);
            Assert.IsFalse(res);           
            Assert.AreEqual(0, test.SecondLevel.Count);
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void CacheDoubleGet()
        {
            var dictionary = new TestElementCache(queueRebalance: a => Task.Factory.StartNew(a));
            var test = new TestElementCache.TestMock(dictionary);

            var key = Guid.NewGuid();
            var val = new TestElement { Updated = 1, Value = key };
            dictionary.Push(key, val);
            dictionary.Push(key, val);
            UpdatableElement<TestElement> fl;         
            var res = test.FirstLevel.TryGetValue(key, out fl);
            Assert.IsFalse(res);
            Assert.AreEqual(1, test.SecondLevel.Count);
            Assert.AreEqual(key, test.SecondLevel[key].Element.Value);
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void CacheDoubleGetWithCollecting()
        {
            var dictionary = new TestElementCache(queueRebalance: a => Task.Factory.StartNew(a));
            var test = new TestElementCache.TestMock(dictionary);

            var key = Guid.NewGuid();
            var val = new TestElement { Updated = 1, Value = key };
            dictionary.Push(key, val);
            dictionary.Push(key, val);
            GC.Collect();
            UpdatableElement<TestElement> fl;
            var res = test.FirstLevel.TryGetValue(key, out fl);

            Assert.IsFalse(res);
            Assert.AreEqual(1, test.SecondLevel.Count);
            Assert.AreEqual(key, test.SecondLevel[key].Element.Value);
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void CacheDoubleGetWithIntermediateCollecting()
        {
            var dictionary = new TestElementCache(queueRebalance: a => Task.Factory.StartNew(a));
            var test = new TestElementCache.TestMock(dictionary);

            var key = Guid.NewGuid();
            var val = new TestElement { Updated = 1, Value = key };
            dictionary.Push(key, val);
            GC.Collect();
            dictionary.Push(key, val);         
            UpdatableElement<TestElement> fl;
            var res = test.FirstLevel.TryGetValue(key, out fl);
            Assert.IsTrue(res);
            Assert.AreEqual(key, fl.Element.Value);
            Assert.AreEqual(0, test.SecondLevel.Count);
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void CacheDoubleGetWithActualityCheck()
        {
            var dictionary = new TestElementCache(queueRebalance: a => Task.Factory.StartNew(a));
            var test = new TestElementCache.TestMock(dictionary);

            var key = Guid.NewGuid();
            var val = new TestElement { Updated = 1, Value = key };
            var newVal = new TestElement { Updated = 2, Value = key };
            dictionary.Push(key, val);
            val = dictionary.RetrieveByFunc(key,(k)=> newVal);
      
            UpdatableElement<TestElement> fl;
            var res = test.FirstLevel.TryGetValue(key, out fl);
            Assert.IsFalse(res);          
            Assert.AreEqual(1, test.SecondLevel.Count);
            Assert.AreEqual(1, val.Updated);
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void CacheTripleGetWithIntermediateActualityDrop()
        {
            var dictionary = new TestElementCache(queueRebalance: a => Task.Factory.StartNew(a));
            var test = new TestElementCache.TestMock(dictionary);

            var key = Guid.NewGuid();
            var val = new TestElement { Updated = 1, Value = key };
            var newVal = new TestElement { Updated = 2, Value = key };
            dictionary.Push(key, val);
            dictionary.Push(key, val);
            dictionary.SetUpdateNecessity(key);
             dictionary.Push(key, newVal);
            val = dictionary.RetrieveByFunc(key, (k) => new TestElement {Updated = 1, Value = key});
            UpdatableElement<TestElement> fl;
            var res = test.FirstLevel.TryGetValue(key, out fl);
            Assert.IsFalse(res);        
            Assert.AreEqual(1, test.SecondLevel.Count);
            Assert.IsFalse(test.SecondLevel[key].NeedUpdate());
            Assert.AreEqual(2, val.Updated);
      
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void CacheQuadrupleGetWithIntermediateActualityDrop()
        {
            var dictionary = new TestElementCache(queueRebalance: a => Task.Factory.StartNew(a));
            var test = new TestElementCache.TestMock(dictionary);

            var key = Guid.NewGuid();
            var val = new TestElement { Updated = 1, Value = key };
            var newVal = new TestElement { Updated = 2, Value = key };
            var newVal2 = new TestElement { Updated = 3, Value = key };
            dictionary.Push( key, val);
            dictionary.Push(key, val);
            dictionary.SetUpdateNecessity(key);
            dictionary.Push(key, newVal);
            val = dictionary.RetrieveByFunc(key,k=> newVal2);
            UpdatableElement<TestElement> fl;
            var res = test.FirstLevel.TryGetValue(key, out fl);
            Assert.IsFalse(res);   
            Assert.AreEqual(1, test.SecondLevel.Count);
            Assert.IsFalse(test.SecondLevel[key].NeedUpdate());
            Assert.AreEqual(2, val.Updated);
          
        }

        private IEnumerable<Guid> GenerateKeysDuringSomeTime(TimeSpan time)
        {
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed < time)
                yield return Guid.NewGuid();
        }

        private IEnumerable<TimeSpan> GenerateSpanDuringSomeTime(TimeSpan time,int averageClearing)
        {
            var watch = Stopwatch.StartNew();
            var rnd = new Random();
            while (watch.Elapsed < time)
                yield return TimeSpan.FromSeconds(rnd.Next(averageClearing));
        }

        private void TestIntensity(GuidCache dictionary, TimeSpan testtime,int intensity)
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var span in GenerateSpanDuringSomeTime(testtime, intensity))
                {
                    Thread.Sleep(span);
                    GC.Collect();
                }
            });


            GenerateKeysDuringSomeTime(testtime)
                .AsParallel()
                .ForAll(k =>
                {
                    var value = dictionary.RetrieveByFunc(k, id => id);
                    if (value != k)
                        Assert.Fail("Некорректно взятая величина");
                });
        }

        [TestMethod]
        [TestCategory("Caching")]
        [TestCategory("LoadTest")]
        [TestCategory("Long")]
        public void CacheDurableTest()
        {
            var testtime = TimeSpan.FromMinutes(5);           
            var dictionary = new GuidCache(queueRebalance: a => Task.Factory.StartNew(a));
            TestIntensity(dictionary, testtime, 10);         
            TestIntensity(dictionary, testtime, 1);
        }
    }
}
