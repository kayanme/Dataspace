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
    public class ComparerCheck
    {

        /// <summary>
        /// Тоже в жепь
        /// </summary>
        [TestCategory("Caching")]
        public void Equality()
        {           

            var comparer = new GuidQuickComparer();
            var watch1 = new Stopwatch();
            var watch2 = new Stopwatch();
            bool outres = true;
            var deflt = EqualityComparer<Guid>.Default;
            for (int i = 0; i < 10000000; i++)
            {
                var key = Guid.NewGuid();
               
                watch1.Start();
                var res1 = deflt.Equals(key, key);
                watch1.Stop();
                watch2.Start();
                var res2 = comparer.Equals(key, key);
                watch2.Stop();
                if (res1 != res2)
                    outres = false;

            }
            Assert.Inconclusive("Times are {0} and {1}", watch1.Elapsed, watch2.Elapsed);
            Assert.IsTrue(outres, "Times are {0} and {1}", watch1.Elapsed, watch2.Elapsed);
        }


        /// <summary>
        /// В жепь.
        /// </summary>
        [TestCategory("Caching")]
        public void Comparing()
        {
            var comparer = new GuidQuickComparer();
            var watch1 = new Stopwatch();
            var watch2 = new Stopwatch();
            bool outres = true;
            for (int i = 0; i < 10000000; i++)
            {
                var key = Guid.NewGuid();
                var key2 = Guid.NewGuid();
                watch1.Start();
                var res1 = Comparer<Guid>.Default.Compare(key, key2);
                watch1.Stop();
                watch2.Start();
                var res2 = comparer.Compare(key, key2);
                watch2.Stop();
                if (res1 != res2)
                    outres = false;
               
            }
            Assert.IsTrue(outres, "Times are {0} and {1}", watch1.Elapsed, watch2.Elapsed);
        }

    }
}
