using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dataspace.Common.Utility.Dictionary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testhelper;

namespace Resources.Test
{
    [TestClass]    
    public class StrictBalancingTest
    {
        
        private CacheNode<int, double> BuildTree(int[] frequences,  int maxFixedBranchDepth = 0)
        {
            var freqSum = frequences.Sum();
            var root = new CacheNode<int, double>(0, (double)frequences[0] / freqSum, maxFixedBranchDepth, Comparer<int>.Default, probabilityCalc: k => (float)k);
            var frs = Enumerable.Range(0, frequences.Length).ToList();
            var rnd = new Random();
            for (int i = 1; i < frequences.Length; i++)
            {
                var t = rnd.Next(frs.Count - 1) + 1;
                var key = frs[t];
                root.AddNode(key, (double)frequences[key] / freqSum, maxFixedBranchDepth, 0);
                frs.RemoveAt(t);
            }
            return root;
        }

        private CacheNode<int, double> BuildAndBalanceTree(int[] frequences, out float expectedFrequency, int maxFixedBranchDepth = 0)
        {
            var root = BuildTree(frequences, maxFixedBranchDepth);          
            var balancer = new CacheNode<int, double>.HeavyRebalancer(root, frequences.Length, 0, 1);         
            var watch = Stopwatch.StartNew();
            expectedFrequency = balancer.Rebalance();
            watch.Stop();
            Debug.Print("{0} balancing was", watch.Elapsed);
            root = balancer.ConstructNewTreeAfterCalculation();
            return root;
        }



        private void TestBalance(int[] frequences)
        {
            var rnd = new Random();
            float expectedFrequency;
            int freqSum = frequences.Sum();
            int sum = freqSum;
            var root = BuildAndBalanceTree(frequences, out expectedFrequency);          
            int totalDepth = 0;
          
            while (sum > 0)
            {
                var key = rnd.Next(frequences.Length);
                if (frequences[key] > 0)
                {
                    int depth = 0;
                    root.FindNode(key, 1, out depth);
                    
                    totalDepth += (depth + 1);
                    frequences[key]--;
                    sum--;
                }
            }

            var actualFrequency = (float)totalDepth / freqSum;           
            Assert.AreEqual(expectedFrequency,actualFrequency,0.00001);
        }

        [TestMethod]
        [TestCategory("Balance")]
        public void TestSimpleBalance()
        {
            var frequences = new[]{1};
            TestBalance(frequences); 
        }

        [TestMethod]
        [TestCategory("Balance")]
        public void TestComplexBalance()
        {
            var frequences = new[] { 1 ,3,67,2,34,5};
            TestBalance(frequences);
        }

        [TestMethod]
        [TestCategory("Balance")]        
        public void TestRandomBalance()
        {
            var frequences = MockHelper.GetNumericRandomSequence(1000, 1000).Select(k=>(int)k).ToArray();             
            TestBalance(frequences);
        }

        [TestMethod]
        [TestCategory("Caching")]
        public void TestTreeDepthLimit()
        {
            const int count = 100;
            const int maxAllowedDepth = 2;

            var tree = BuildTree(Enumerable.Range(0, count).ToArray(), maxAllowedDepth);
                for (int i = 0; i < count; i++)
                {
                    GC.Collect(2, GCCollectionMode.Forced, true); 
                    GC.WaitForPendingFinalizers();
                }
                var maxDepth =
                Enumerable.Range(0, count).Max(a =>
                                                 {
                                                     int depth;
                                                     tree.FindNode(a, 0, out depth);
                                                     return depth;
                                                 });
            Assert.IsTrue(maxDepth <= maxAllowedDepth);
        }

        [TestMethod]
        [TestCategory("Balance")]
        public void TestTreeDepthLimitAfterBalance()
        {
            const int count = 30;
            const int maxAllowedDepth = 5;
            float d;
            int p = 0;

            var tree =
                new Func<CacheNode<int, double>>(
                    () => BuildAndBalanceTree(Enumerable.Range(0, count).ToArray(), out d, maxAllowedDepth))();
                //тоже мусор не всегд собирается

            for (int i = 0; i < count; i++)
            {
                Collect();
            }



            var maxDepth =
                Enumerable.Range(0, count).Max(a =>
                                                   {
                                                       int depth;
                                                       tree.FindNode(a, 1, out depth);
                                                       return depth;
                                                   });

            Assert.IsTrue(maxDepth <= maxAllowedDepth);

        }


        private void Collect()
        {
            GC.Collect(2, GCCollectionMode.Forced, true); 
            GC.WaitForPendingFinalizers();
        }

        [TestMethod]
        [TestCategory("Balance")]
        public void TestBalancingAnTreeAfterGarbageCollecting()
        {

            var frequences = new[] {1, 2, 3, 400, 700, 600, 70, 68, 67, 59, 56, 55, 58, 57, 56, 63};
            //частоты заданы с таким расчетом, чтобы редкие занимали нижние этажи
            float exp;
            var root = new Func<CacheNode<int, double>>(() => BuildAndBalanceTree(frequences, out exp, 2))();
            Collect();
            Collect(); 
            Collect();
            Collect();
            Collect();

            int depth = 0;
            var maxDepth =
                Enumerable.Range(0, frequences.Count()).Max(a =>
                                                                {
                                                                    root.FindNode(a, 1, out depth);
                                                                    return depth;
                                                                });

            var deletedNode = root.FindNode(0, 1, out depth);
            Assert.AreEqual(0, deletedNode);
            deletedNode = root.FindNode(1, 1, out depth);
            Assert.AreEqual(0, deletedNode);
            deletedNode = root.FindNode(2, 1, out depth);
            Assert.AreEqual(0, deletedNode);
            deletedNode = root.FindNode(3, 1, out depth);
            Assert.AreNotEqual(0, deletedNode);


        }

    }
}
