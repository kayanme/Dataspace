using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Utility.Dictionary;
using Dataspace.Common.Utility;
using Dataspace.Common.Utility.Dictionary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Resources.Test.Caching
{
    [TestClass]
    public class AccumulatingTestWithCache
    {
        [Serializable]
        public class TestClass
        {
            public string Test;
        }

        [TestMethod]
        public void TestAccumulatingWithCache()
        {
            var cache =
                new UpgradedCache
                    <Guid, TestClass, NoReferenceUpdatableElement<TestClass>>(
                    Comparer<Guid>.Default,  EqualityComparer<Guid>.Default,
                                                                a => a());
            var acc = new Accumulator<Guid, TestClass>(
                cache.Push, 
                cache.HasActualValue,
                k => cache.RetrieveByFunc(k, o => new TestClass {Test = k.ToString()}),
                k => k.ToDictionary(k2 => k2, k2 => new TestClass {Test = k2.ToString()}));
            var count = 100000;
            var keys = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();

            var t1 = Get(acc, keys.Take(count/3));

            var res1 = t1.ContinueWith(tt =>
                                           {
                                               GC.Collect(2, GCCollectionMode.Forced);
                                               return Get2(tt.Result);
                                           });
            var t2 = t1.ContinueWith(tt2 => Get(acc, keys.Skip(count/3).Take(count/3))
                                                .ContinueWith(t =>
                                                                  {
                                                                      GC.Collect
                                                                          (2,  GCCollectionMode.Forced);
                                                                      return  Get2(t.Result);
                                                                  }));

            var res3 = Get(acc, keys.Skip(count/3*2).ToArray())
                             .ContinueWith(t =>
                                  {
                                      GC.Collect(2, GCCollectionMode.Forced);
                                      return Get2(t.Result);
                                  });

            Assert.AreEqual(res1.Result.Count(k => k!=null),count/3);
            Assert.AreEqual(t2.Result.Result.Count(k => k != null), count/3);
            Assert.AreEqual(res3.Result.Count(k => k != null), count -  count / 3*2);
        }

        private Task<IEnumerable<Func<TestClass>>> Get(Accumulator<Guid, TestClass> acc,IEnumerable<Guid> keys )
        {
            return Task.Factory.StartNew(()=> keys.Select(acc.GetValue).ToArray().AsEnumerable());                                            
        }

        private IEnumerable<TestClass> Get2(IEnumerable<Func<TestClass>> lazies)
        {
            return lazies.Select(k=>k()).ToArray();
        }
    }
}
