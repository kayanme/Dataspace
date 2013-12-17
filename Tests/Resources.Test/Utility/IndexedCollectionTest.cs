using System;
using System.Collections.Generic;
using System.Linq;
using Dataspace.Common.Utility;
using Dataspace.Common.Utility.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace A.Resources.Test.Utility
{
    [TestClass]
    public class IndexedCollectionTest
    {
        [TestMethod]
        [TestCategory("IndexingCollection")]
        public void SimpleTest1()
        {
            Queries.Debug = true;
            Queries.TimesWasHere = 0;
            var collection = new IndexedCollection<KeyValuePair<int,int>>(k=>k.Key)
            { new KeyValuePair<int, int>(1, 3), new KeyValuePair<int, int>(5, 2) };
            var items = collection.Where(k => k.Key == 5).ToArray();
            Assert.AreEqual(1,Queries.TimesWasHere);
            Assert.AreEqual(1,items.Length);
            Assert.AreEqual(2, items[0].Value);
        }


        [TestMethod]
        [TestCategory("IndexingCollection")]
        public void SimpleTest2()
        {
            Queries.Debug = true;
            Queries.TimesWasHere = 0;
            var collection = new IndexedCollection<KeyValuePair<int, int>>(k => k.Key) { new KeyValuePair<int, int>(1, 3), new KeyValuePair<int, int>(5, 2), new KeyValuePair<int, int>(5, 3) };
            var items = collection.Where(k => k.Key == 5 && k.Value == 3).ToArray();
            Assert.AreEqual(1, Queries.TimesWasHere);
            Assert.AreEqual(1, items.Length);
            Assert.AreEqual(5, items[0].Key);
            Assert.AreEqual(3, items[0].Value);
        }

        [TestMethod]
        [TestCategory("IndexingCollection")]
        public void SimpleTest3()
        {
            Queries.Debug = true;
            Queries.TimesWasHere = 0;
            var collection = new IndexedCollection<KeyValuePair<int, int>>(k => k.Key) { new KeyValuePair<int, int>(1, 3), new KeyValuePair<int, int>(5, 2), new KeyValuePair<int, int>(5, 3) };
            int value = 5;
            var items = collection.Where(k => k.Key == value && k.Value == 3).ToArray();
            Assert.AreEqual(1, Queries.TimesWasHere);
            Assert.AreEqual(1, items.Length);
            Assert.AreEqual(5, items[0].Key);
            Assert.AreEqual(3, items[0].Value);
        }

        [TestMethod]
        [TestCategory("IndexingCollection")]
        public void SimpleTest4()
        {
            Queries.Debug = true;
            Queries.TimesWasHere = 0;
            var collection = new IndexedCollection<KeyValuePair<int, int>>(k => k.Key) { new KeyValuePair<int, int>(0, 3), new KeyValuePair<int, int>(5, 2), new KeyValuePair<int, int>(5, 3) };
          
            var items = collection.Where(k => k.Key == new int() && k.Value == 3).ToArray();
            Assert.AreEqual(1, Queries.TimesWasHere);
            Assert.AreEqual(1, items.Length);
            Assert.AreEqual(0, items[0].Key);
            Assert.AreEqual(3, items[0].Value);
        }

        [TestMethod]
        [TestCategory("IndexingCollection")]
        public void SimpleTest5()
        {
            Queries.Debug = true;
            Queries.TimesWasHere = 0;
            var collection = new IndexedCollection<KeyValuePair<object, int>>(k => k.Key)
                                 {
                                     new KeyValuePair<object, int>(0, 3),
                                     new KeyValuePair<object, int>(null, 2),
                                     new KeyValuePair<object, int>(5, 3)
                                 };
            int v = 5;
            var items = collection.Where(k => k.Key != null && (int)k.Key == v && k.Value == 3).ToArray();
           // Assert.AreEqual(1, Queries.TimesWasHere);
            Assert.AreEqual(1, items.Length);
            Assert.AreEqual(5, items[0].Key);
            Assert.AreEqual(3, items[0].Value);
        //    Assert.Fail("Пока не проходит");
        }

        [TestMethod]
        [TestCategory("IndexingCollection")]
        public void SimpleTest6()
        {
            Queries.Debug = true;
            Queries.TimesWasHere = 0;
            Func<KeyValuePair<int, int>, int> counter = k => k.Key + k.Value;
            var collection = new IndexedCollection<KeyValuePair<int, int>>(k=>counter(k))
                                 {
                                     new KeyValuePair<int, int>(0, 3),
                                     new KeyValuePair<int, int>(2, 2),
                                     new KeyValuePair<int, int>(5, 3)
                                 };

            var items = collection.Where(k => counter(k) == 4).ToArray();
            Assert.AreEqual(1, Queries.TimesWasHere);
            Assert.AreEqual(1, items.Length);
            Assert.AreEqual(2, items[0].Key);
            Assert.AreEqual(2, items[0].Value);
        }

        private class TimeRange
        {
            public DateTime StartTime;
            public DateTime EndTime;
            public int Value;

            public TimeRange(DateTime start,DateTime end,int val)
            {
                StartTime = start;
                EndTime = end;
                Value = val;
            }
        }

        private class StringTest
        {
            public string Key;
            public string Value;
           
            public StringTest(string key,string val)
            {
                Key = key;
                Value = val;
            }
        }

        [TestMethod]
        [TestCategory("IndexingCollection")]
        public void AdvancedTest1()
        {
            Queries.Debug = true;
            Queries.TimesWasHere = 0;
            var collection = new IndexedCollection<TimeRange>(k => k.Value,k=>k.EndTime - k.StartTime)
                                 {
                                     new TimeRange(new DateTime(2012,2,1),new DateTime(2012,2,2), 4 ),
                                     new TimeRange(new DateTime(2012,2,3),new DateTime(2012,2,4), 5 ),
                                     new TimeRange(new DateTime(2012,2,4),new DateTime(2012,2,5), 6 ),
                                     new TimeRange(new DateTime(2012,2,2),new DateTime(2012,2,6), 7 ),
                                     new TimeRange(new DateTime(2012,2,2),new DateTime(2012,2,8), 8 ),
                                     new TimeRange(new DateTime(2012,2,5),new DateTime(2012,2,10),9 ),
                                     new TimeRange(new DateTime(2012,2,4),new DateTime(2012,2,9), 10 ),
                                     new TimeRange(new DateTime(2012,2,2),new DateTime(2012,2,3), 11 ),
                                     new TimeRange(new DateTime(2012,2,8),new DateTime(2012,2,10), 12 ),
                                 };
            var items = collection.Where( k => k.Value == 4 || k.StartTime == new DateTime(2012, 2, 2)).ToArray();
            Assert.AreEqual(0, Queries.TimesWasHere);
            Assert.AreEqual(4, items.Length);         

            Queries.TimesWasHere = 0;
            items = collection.Where( k => k.Value == 4 && k.EndTime == new DateTime(2012, 2, 2)).ToArray();
            Assert.AreEqual(1, Queries.TimesWasHere);
            Assert.AreEqual(1, items.Length);
            Assert.AreEqual(4, items[0].Value);

            Queries.TimesWasHere = 0;
            items = collection.Where( k => k.Value == 5 && k.StartTime != new DateTime(2012, 2, 3)).ToArray();
            Assert.AreEqual(1, Queries.TimesWasHere);
            Assert.AreEqual(1, items.Length);

            Queries.TimesWasHere = 0;
            items = collection.Where( k => k.EndTime - k.StartTime == TimeSpan.FromDays(1)).ToArray();
            Assert.AreEqual(1, Queries.TimesWasHere);
            Assert.AreEqual(4, items.Length);
        }

        [TestMethod]
        [TestCategory("IndexingCollection")]
        public void StringEqualityTest1()
        {
            Queries.Debug = true;
            Queries.TimesWasHere = 0;
            var collection = new IndexedCollection<StringTest>(k => k.Key)
                                 {
                                     new StringTest("1", "!"),
                                     new StringTest("2", "@"),
                                 };
        }
    }
}
