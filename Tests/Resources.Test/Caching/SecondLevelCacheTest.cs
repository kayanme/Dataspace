using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Utility.Dictionary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cache = Dataspace.Common.Utility.Dictionary.SecondLevelCache<int, int>;

namespace Resources.Test
{
    [TestClass]
    public class SecondLevelCacheTest
    {

        [TestMethod]
        [TestCategory("Caching")]
        public void DumbTest()
        {
            
            var node = new CacheNode<int, int>(10, 10,0,  Comparer<int>.Default);
            int depth = 0;
            Assert.AreEqual(10, node.FindNode(10, 0, out depth));
            Assert.AreEqual(0, depth);
            Assert.AreEqual(0, node.FindNode(9, 0, out depth));
            Assert.AreEqual(0, node.FindNode(11, 0, out depth));
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void Add1Test()
        {
            var node = new CacheNode<int, int>(10, 10, 0, Comparer<int>.Default);
            node.AddNode(11, 11, 0, 0);
            int depth = 0;
            Assert.AreEqual(11, node.FindNode(11, 0, out depth));
            Assert.AreEqual(depth, 1);
        }


        [TestMethod]
        [TestCategory("Caching")]
        public void Add2Test()
        {
            var node = new CacheNode<int, int>(10, 10, 0, Comparer<int>.Default);
            int depth = 0;
            node.AddNode(9, 9, 0, 0);
            Assert.AreEqual(9, node.FindNode(9, 0, out depth));
            Assert.AreEqual(depth, 1);
        }


        [TestMethod]
        [TestCategory("Caching")]
        public void Add3Test()
        {
            var node = new CacheNode<int, int>(10, 10, 0, Comparer<int>.Default);
            node.AddNode(11, 11, 0, 0);
            node.AddNode(9, 9, 0, 0);
            int depth = 0;
            Assert.AreEqual(9, node.FindNode(9, 0, out depth));
            Assert.AreEqual(1, depth);          
            Assert.AreEqual(11, node.FindNode(11, 0, out depth));
            Assert.AreEqual(1, depth);
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void AddWithCollectingTest()
        {
           
            var node = new CacheNode<int, int>(10, 10, 0, Comparer<int>.Default);
            node.AddNode(11, 11, 0, 0);
            node.AddNode(9, 9, 0, 0);
            int depth;
            Assert.AreEqual(9, node.FindNode(9, 0, out depth));
            Assert.AreEqual(1, depth);;
            Assert.AreEqual(11, node.FindNode(11, 0, out depth));
            Assert.AreEqual(1, depth);
            GC.Collect();
            GC.Collect();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.AreEqual(0, node.FindNode(9, 0, out depth));
            Assert.AreEqual(0, node.FindNode(11, 0, out depth));
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void DeepAddWithCollectingTest()
        {

            var node = new CacheNode<int, int>(10, 10, 0, Comparer<int>.Default);

            node.AddNode(8, 8, 0, 0);
            node.AddNode(7, 7, 0, 0);
            node.AddNode(9, 9, 0, 0);
            node.AddNode(12, 12, 0, 0);
            node.AddNode(11, 11, 0, 0);
            node.AddNode(13, 13, 0, 0);
            int depth = 0;
            Assert.AreEqual(7, node.FindNode(7, 0, out depth));
            Assert.AreEqual(2, depth);
            Assert.AreEqual(8, node.FindNode(8, 0, out depth));
            Assert.AreEqual(1, depth);
            Assert.AreEqual(9, node.FindNode(9, 0, out depth));
            Assert.AreEqual(2, depth);
            Assert.AreEqual(10, node.FindNode(10, 0, out depth));
            Assert.AreEqual(0, depth);
            Assert.AreEqual(11, node.FindNode(11, 0, out depth));
            Assert.AreEqual(2, depth);
            Assert.AreEqual(12, node.FindNode(12, 0, out depth));
            Assert.AreEqual(1, depth);
            Assert.AreEqual(13, node.FindNode(13, 0, out depth));
            Assert.AreEqual(2, depth);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.AreEqual(0, node.FindNode(7, 0, out depth));
            Assert.AreEqual(8, node.FindNode(8, 0, out depth));
            Assert.AreEqual(1, depth);
            Assert.AreEqual(0, node.FindNode(9, 0, out depth));
            Assert.AreEqual(10, node.FindNode(10, 0, out depth));
            Assert.AreEqual(0, depth);
            Assert.AreEqual(0, node.FindNode(11, 0, out depth));
            Assert.AreEqual(12, node.FindNode(12, 0, out depth));
            Assert.AreEqual(1, depth);
            Assert.AreEqual(0, node.FindNode(13, 0, out depth));
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.AreEqual(0, node.FindNode(7, 0, out depth));
            Assert.AreEqual(0, node.FindNode(8, 0, out depth));
            Assert.AreEqual(0, node.FindNode(9, 0, out depth));
            Assert.AreEqual(10, node.FindNode(10, 0, out depth));
            Assert.AreEqual(0, depth);
            Assert.AreEqual(0, node.FindNode(11, 0, out depth));
            Assert.AreEqual(0, node.FindNode(12, 0, out depth));
            Assert.AreEqual(0, node.FindNode(13, 0, out depth));
        }


        [TestMethod]
        [TestCategory("Caching")]
        public void DeepAddWithCollectingWithNonZeroFixedBranchLengthTest()
        {

            var node = new CacheNode<int, int>(10, 10,1, Comparer<int>.Default);
            #region Prepare
            node.AddNode(8, 8, 1, 0);
            node.AddNode(7, 7, 1, 0);
            node.AddNode(9, 9, 1, 0);
            node.AddNode(12, 12, 1, 0);
            node.AddNode(11, 11, 1, 0);
            node.AddNode(13, 13, 1, 0);
            int depth = 0;
            Assert.AreEqual(7, node.FindNode(7, 0, out depth));
            Assert.AreEqual(2, depth);
            depth = 0;
            Assert.AreEqual(8, node.FindNode(8, 0, out depth));
            Assert.AreEqual(1, depth);
            depth = 0;
            Assert.AreEqual(9, node.FindNode(9, 0, out depth));
            Assert.AreEqual(2, depth);
            depth = 0;
            Assert.AreEqual(10, node.FindNode(10, 0, out depth));
            Assert.AreEqual(0, depth);
            depth = 0;
            Assert.AreEqual(11, node.FindNode(11, 0, out depth));
            Assert.AreEqual(2, depth);
            depth = 0;
            Assert.AreEqual(12, node.FindNode(12, 0, out depth));
            Assert.AreEqual(1, depth);
            depth = 0;
            Assert.AreEqual(13, node.FindNode(13, 0, out depth));
            Assert.AreEqual(2, depth);
            depth = 0;
            #endregion

            #region First collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.AreEqual(0, node.FindNode(7, 0, out depth));
            depth = 0;
            Assert.AreEqual(8, node.FindNode(8, 0, out depth));
            Assert.AreEqual(1, depth);
            depth = 0;
            Assert.AreEqual(0, node.FindNode(9, 0, out depth));
            depth = 0;
            Assert.AreEqual(10, node.FindNode(10, 0, out depth));
            Assert.AreEqual(0, depth);
            depth = 0;
            Assert.AreEqual(0, node.FindNode(11, 0, out depth));
            depth = 0;
            Assert.AreEqual(12, node.FindNode(12, 0, out depth));
            Assert.AreEqual(1, depth);
            depth = 0;
            Assert.AreEqual(0, node.FindNode(13, 0, out depth));
            #endregion

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.AreEqual(0, node.FindNode(7, 0, out depth));
            Assert.AreEqual(8, node.FindNode(8, 0, out depth));
            Assert.AreEqual(0, node.FindNode(9, 0, out depth));
            depth = 0;
            Assert.AreEqual(10, node.FindNode(10, 0, out depth));
            Assert.AreEqual(0, depth);
            Assert.AreEqual(0, node.FindNode(11, 0, out depth));
            Assert.AreEqual(12, node.FindNode(12, 0, out depth));
            Assert.AreEqual(0, node.FindNode(13, 0, out depth));
        }

    }
}
