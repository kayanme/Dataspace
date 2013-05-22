using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;

namespace Dataspace.Common.Utility
{

    [Obsolete("Use Accumulator instead")]
    public class AccumulatingProvider<TKey, TValue>
    {
        private int _curStamp;

        private const int _maxKeyCount = 500;

      
        private ManualResetEventSlim _listLock = new ManualResetEventSlim();


        private ConcurrentDictionary<int, Dictionary<TKey, TValue>> _accumulated = new ConcurrentDictionary<int, Dictionary<TKey, TValue>>();
        private ConcurrentDictionary<int, int> _loadedValues = new ConcurrentDictionary<int, int>();
        private ConcurrentDictionary<int, ConcurrentQueue<TKey>> _getStack = new ConcurrentDictionary<int, ConcurrentQueue<TKey>>();
       
        private IEqualityComparer<TKey> _equalityComparer;

        private Func<IEnumerable<TKey>, IEnumerable<KeyValuePair<TKey, TValue>>> _getter;


        private readonly Func<TKey, bool> _hasElement;


        public AccumulatingProvider(
            
                            Func<IEnumerable<TKey>, IEnumerable<KeyValuePair<TKey, TValue>>> getter,
                            Func<TKey, bool> hasElement,
                             IEqualityComparer<TKey> equalityComparer = null)
        {
            _listLock.Set();
            _hasElement = hasElement;
            _getter = getter;
            _equalityComparer = equalityComparer;
            _curStamp = 0;
            _loadedValues.TryAdd(_curStamp, 0);
            _getStack.TryAdd(_curStamp, new ConcurrentQueue<TKey>());
        }

        private void LoadPack(int stamp)
        {
            Contract.Requires(_getStack!=null);
            ConcurrentQueue<TKey> t3;
            var idsToGet = _getStack[stamp].Distinct().Where(k => !_hasElement(k)).ToArray();
            var vals = _getter(idsToGet).ToDictionary(k => k.Key, k => k.Value, _equalityComparer);
           _loadedValues[stamp] = _getStack[stamp].Count;
           _accumulated.TryAdd(stamp, vals);
           _getStack.TryRemove(stamp, out t3);
         
        }

        private Func<TValue> LazyGetter(TKey key, int stamp)
        {
            return () =>
                       {

                           TValue res;
                           lock (_loadedValues)
                           {

                               RiseStamp(stamp);
                               if (_loadedValues[stamp] == 0)
                                   LoadPack(stamp);
                           }
                           // Debug.Assert(_accumulated[stamp].ContainsKey(key)); - ниже вообще-то костыль, хотя в целом, мне кажется, вполне приемлемый.
                           if (!_accumulated[stamp].ContainsKey(key))
                               res = _getter(new[] { key }).First().Value;
                           else
                           {
                               res = _accumulated[stamp][key];

                               _accumulated[stamp].Remove(key);
                               _loadedValues[stamp]--;
                           }
                           if (_loadedValues[stamp]==0)
                           {
                               Dictionary<TKey, TValue> t;
                               
                               int t2;
                               _accumulated.TryRemove(stamp,out t);
                               _loadedValues.TryRemove(stamp,out t2);
                          
                           }
                           return res;
                       };

        }

        private void RiseStamp(int stamp)
        {
            _listLock.Reset();
            _loadedValues.TryAdd(stamp + 1, 0);
            _getStack.TryAdd(stamp + 1, new ConcurrentQueue<TKey>());
            Interlocked.CompareExchange(ref _curStamp, _curStamp + 1, stamp);
            _listLock.Set();
        }
      
        public Func<TValue> GetValue(TKey key)
        {
           
            var stamp = _curStamp;
            if (_getStack[stamp].Count > _maxKeyCount)
            {
                RiseStamp(stamp);
            }
            _listLock.Wait();
            stamp = _curStamp;
            try
            {
                _getStack[stamp].Enqueue(key);
            }
            catch (KeyNotFoundException)
            {
                Debugger.Launch();
                Debugger.Break();
            }
          
            return LazyGetter(key, stamp);
                                        
        }
    }
}
