using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Utility.Dictionary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Resources.Test.Caching
{
    [TestClass]
    public unsafe class ComparerCheck
    {


        private unsafe Guid FromLong(long i)
        {
            return *((Guid*)&i);
        }


        private const int count = 10000;

        ///// <summary>
        ///// Тоже в жепь
        ///// </summary>
        //[TestCategory("Caching")]
        //public void Equality()
        //{           

        //    var comparer = new GuidQuickComparer();
        //    var watch1 = new Stopwatch();
        //    var watch2 = new Stopwatch();
        //    bool outres = true;
        //    var deflt = EqualityComparer<Guid>.Default;
        //    for (int i = 0; i < 10000000; i++)
        //    {
        //        var key = Guid.NewGuid();
               
        //        watch1.Start();
        //        var res1 = deflt.Equals(key, key);
        //        watch1.Stop();
        //        watch2.Start();
        //        var res2 = comparer.Equals(key, key);
        //        watch2.Stop();
        //        if (res1 != res2)
        //            outres = false;

        //    }
        //    Assert.Inconclusive("Times are {0} and {1}", watch1.Elapsed, watch2.Elapsed);
        //    Assert.IsTrue(outres, "Times are {0} and {1}", watch1.Elapsed, watch2.Elapsed);
        //}


        ///// <summary>
        ///// В жепь.
        ///// </summary>
        //[TestCategory("Caching")]
        //public void Comparing()
        //{
        //    var comparer = new GuidQuickComparer();
        //    var watch1 = new Stopwatch();
        //    var watch2 = new Stopwatch();
        //    bool outres = true;
        //    for (int i = 0; i < 10000000; i++)
        //    {
        //        var key = Guid.NewGuid();
        //        var key2 = Guid.NewGuid();
        //        watch1.Start();
        //        var res1 = Comparer<Guid>.Default.Compare(key, key2);
        //        watch1.Stop();
        //        watch2.Start();
        //        var res2 = comparer.Compare(key, key2);
        //        watch2.Stop();
        //        if (res1 != res2)
        //            outres = false;
               
        //    }
        //    Assert.IsTrue(outres, "Times are {0} and {1}", watch1.Elapsed, watch2.Elapsed);
        //}

         [TestMethod]
        public void Time()
        {
            var key = Enumerable.Range(0, 128).Select(k => (long)Math.Pow(2, (127 - k))).ToArray();
            Comparer<Guid> comp = new GuidQuickComparer(key);
            CheckTime(comp);
            comp = Comparer<Guid>.Default;
            CheckTime(comp);
        }

        [TestMethod]
        public void Test()
        {          
            var key = Enumerable.Range(0, 128).Select(k => (long)Math.Pow(2, (127 - k))).ToArray();         
            Check(key);
        }

        private void Check(long[] key)
        {
            var comp = new GuidQuickComparer(key);
            var keys = Enumerable.Range(0, count).Select(k => Guid.NewGuid()).ToArray();
            var orderedKey = keys.OrderBy(k => k, comp).ToArray();

            var res = Enumerable.Range(0, count).All((i) =>
                                           Enumerable.Range(0, count).All(
                                               (i2) =>
                                               (i < i2)
                                                   ? (comp.Compare(orderedKey[i], orderedKey[i2]) <= 0)
                                                   : (comp.Compare(orderedKey[i], orderedKey[i2]) >= 0)));
            Assert.IsTrue(res);
        }

        private void CheckTime(IComparer<Guid> comp)
        {

            var sw = new Stopwatch();
            for (int i = 0; i < 100; i++)
            {
                var keys = Enumerable.Range(0, count).Select(k => Guid.NewGuid()).ToArray();
                sw.Start();
                var orderedKey = keys.OrderBy(k => k, comp).ToArray();
                sw.Stop();
            }
           Assert.Inconclusive((sw.ElapsedMilliseconds / 100).ToString());
        }
    }
}
