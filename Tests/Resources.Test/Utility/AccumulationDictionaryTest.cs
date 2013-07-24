using System;
using System.Collections.Concurrent;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dataspace.Common;
using Dataspace.Common.Utility;
using Common.Utility;
using Testhelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Resources.Test
{
    [TestClass]
    public class AccumulationDictionaryTest
    {

        private class Store
        {
            public Guid Key;
            public string Name;
        }

        private ConcurrentDictionary<Guid,Store> _repository;

      

        private Store Get(Guid id)
        {
            return _repository[id];
        }

        private  Accumulator<Guid,Store> Initialize(int count)
        {
            _repository = new ConcurrentDictionary<Guid, Store>(
                MockHelper.RandomPairs<Guid, string>(count)
                          .Select(k => new Store {Key = k.Item1, Name = k.Item2})
                          .ToDictionary(k => k.Key));
            var accDict = new Accumulator<Guid, Store>(
                (k,v,t)=>_repository.AddOrUpdate(k,v,(l,m)=>v),
                _repository.ContainsKey,
                key=>_repository[key],
                k=>k.Select(Get).ToDictionary(k2=>k2.Key));
            return accDict;
        }

        private void CheckValues(IEnumerable<KeyValuePair<Guid, Store>> values)
        {
            Assert.IsTrue(values.All(k => k.Key == k.Value.Key));
            Assert.IsTrue(values.All(k => Get(k.Value.Key).Name == k.Value.Name));
        }

        private void CheckValues(IEnumerable<KeyValuePair<Guid, Lazy<Store>>> values)
        {
            Assert.IsTrue(values.All(k => k.Key == k.Value.Value.Key));
            Assert.IsTrue(values.All(k => Get(k.Value.Value.Key).Name == k.Value.Value.Name));
        }

        [TestMethod]
        [TestCategory("AccumulatingDictionary")]
        public void Basic()
        {
            var dict = Initialize(5000);

            var values = _repository.Select(k => new {key = k.Key, value = dict.GetValue(k.Key)()})
                                    .ToDictionary(k=>k.key,k=>k.value);
            CheckValues(values);

            values = _repository.Select(k => new { key = k.Key, value = dict.GetValue(k.Key) }).ToArray()
                                    .ToDictionary(k => k.key, k => k.value());

            CheckValues(values);

            var lazyValues = _repository.Select(k => new { key = k.Key, value = dict.GetValue(k.Key) }).ToArray()
                                        .ToDictionary(k => k.key, k => k.value());

            CheckValues(lazyValues);
        }


      

        [TestMethod]
        [TestCategory("AccumulatingDictionary")]
        public void SequencesPartititions()
        {
            var dict = Initialize(5000);

            var values1 = _repository.Take(500).Select(k => new { key = k.Key, value = dict.GetValue(k.Key)() }).ToDictionary(k => k.key, k => k.value);
            var values2 = _repository.Skip(500).Take(1000).Select(k => new { key = k.Key, value = dict.GetValue(k.Key)() }).ToDictionary(k => k.key, k => k.value);
            var values3 = _repository.Skip(1500).Take(1500).Select(k => new { key = k.Key, value = dict.GetValue(k.Key)() }).ToDictionary(k => k.key, k => k.value);

            CheckValues(values1);
            CheckValues(values2);
            CheckValues(values3);

            values1 = _repository.Take(500).Select(k => new { key = k.Key, value = dict.GetValue(k.Key) }).ToArray().ToDictionary(k => k.key, k => k.value());
            values2 = _repository.Skip(500).Take(1000).Select(k => new { key = k.Key, value = dict.GetValue(k.Key) }).ToArray().ToDictionary(k => k.key, k => k.value());
            values3 = _repository.Skip(1500).Take(1500).Select(k => new { key = k.Key, value = dict.GetValue(k.Key) }).ToArray().ToDictionary(k => k.key, k => k.value());

            CheckValues(values1);
            CheckValues(values2);
            CheckValues(values3);

            var lazyValues1 = _repository.Take(500).Select(k => new { key = k.Key, value = dict.GetValue(k.Key) }).ToDictionary(k => k.key, k => new Lazy<Store>(k.value));
            var lazyValues2 = _repository.Skip(500).Take(1000).Select(k => new { key = k.Key, value = dict.GetValue(k.Key) }).ToDictionary(k => k.key, k =>new Lazy<Store>(k.value));
            var lazyValues3 = _repository.Skip(1500).Take(1500).Select(k => new { key = k.Key, value = dict.GetValue(k.Key) }).ToDictionary(k => k.key, k => new Lazy<Store>(k.value));

            CheckValues(lazyValues1);
            CheckValues(lazyValues2);
            CheckValues(lazyValues3);
           
        }

        [TestMethod]
        [TestCategory("AccumulatingDictionary")]
        public void ParallelPartitions()
        {
            const int count = 2000;
            const int seqLength = 2000;
            const int seqCount = 15;

            var dict = Initialize(count);
            var indexedRepository = _repository.ToArray();

            var valIndexes =
                seqCount.Times().Select(k => MockHelper.GetNumericRandomSequence(seqLength, count).Distinct().ToArray()).ToArray();
            var valKeys = valIndexes.Select(k => k.Select(k2 => indexedRepository[k2].Key).ToArray()).ToArray();

            var delays = MockHelper.GetNumericRandomSequence(seqCount, 10).ToArray();

            var sequences =
                valKeys.AsParallel()    
                       .WithDegreeOfParallelism(seqCount)
                       .Select((k, i) =>
                                {
                                    Thread.Sleep((int) delays[i]);
                                    TimeSpan time;
                                    var l = MockHelper.MeasureOperation(() => k.Select(dict.GetValue).ToArray(), out time);

                                    return l.Select(k2 => k2()).ToDictionary(k2 => k2.Key);
                                })
                       .ToArray();

            foreach (var sequence in sequences)
            {
                CheckValues(sequence);
            }
        }
    }
}
