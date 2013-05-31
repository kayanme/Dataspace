using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Dataspace.Common.Statistics;
using Dataspace.Common.Data;
using Dataspace.Common.Statistics;
using Dataspace.Common.Interfaces.Internal;
using Common.Utility.Dictionary;

namespace Dataspace.Common.Utility.Dictionary
{
   
    internal class SecondLevelCache<TKey,TValue>
    {
        internal class MockChannel : IStatChannel
        {
            public void Register(string resourceType)
            {

            }

            public void SendMessageAboutOneResource(Guid id, Actions action, TimeSpan length = new TimeSpan())
            {

            }

            public void SendMessageAboutMultipleResources(Guid[] ids, Actions action, TimeSpan length = new TimeSpan())
            {

            }
        };
       
        internal IStatChannel Channel = new MockChannel();

        private readonly IComparer<TKey> _comparer = Comparer<TKey>.Default;
        private readonly IEqualityComparer<TValue> _eqComparer = EqualityComparer<TValue>.Default;

        private CacheNode<TKey, TValue> _root;
       
        private int _cacheNodeCount;

        private long _totalAccessDepth = 0;
        private long _getCount = 0;
        private float _expectedGet = 1;
        private int _currentPath = 0;
        private int _maxFixedBranchDepth = 10;

        private readonly Action<Action> _queueRebalance;
        private readonly Func<TValue, float> _frequencyCalc;

        private int _rebalancingQueued;

        private const int Queued = 1;
        private const int NotQueued = 0;

        public const int CheckThreshold = 100;
        public const float RebalancingDelta = 0.05F;

        public class CacheState
        {
            private readonly SecondLevelCache<TKey, TValue> _cache;

            public float Rate { get { return (float) _cache._totalAccessDepth/_cache._getCount; } }
            public float ExpectedRate { get { return _cache._expectedGet; } }
            public int CurrentPath { get { return _cache._currentPath; } }
            public bool RebalancingQueued { get { return _cache._rebalancingQueued == Queued; } }
            public int MaxFixedBranchDepth { get { return _cache._maxFixedBranchDepth; } internal set { _cache._maxFixedBranchDepth = value; } }
            public int CheckThreshold { get { return SecondLevelCache<TKey, TValue>.CheckThreshold; } }
            public float RebalancingDelta { get { return SecondLevelCache<TKey, TValue>.RebalancingDelta; } }
        

            public CacheState(SecondLevelCache<TKey,TValue> cache)
            {
                _cache = cache;
            }
        }

        private readonly CacheState _state;

        public CacheState State
        {
            get { return _state; }
        }
      
        public int Count
        {
            get { return _cacheNodeCount; }
        }

        private void QueueRebalance()
        {
            if (Interlocked.CompareExchange(ref _rebalancingQueued, Queued, NotQueued) == NotQueued)
            {
                Channel.SendMessageAboutOneResource(Guid.Empty, Actions.RebalanceQueued);
            
                _queueRebalance(() =>
                                    {
                                        Channel.SendMessageAboutOneResource(Guid.Empty, Actions.RebalanceStarted);
                                        var time = Stopwatch.StartNew();
                                       
                                        bool lockTaken = false;
                                        try
                                        {
                                            _writeLock.Enter(ref lockTaken);
                                            var rebalancer = CreateRebalancer();
                                            _expectedGet = rebalancer.Rebalance();                                           
                                            _root = rebalancer.ConstructNewTreeAfterCalculation();
                                            _currentPath = rebalancer.OutPath;
                                        }
                                        finally
                                        {

                                            if (lockTaken)
                                                _writeLock.Exit();
                                        }
                                        time.Stop();
                                        Channel.SendMessageAboutOneResource(Guid.Empty, Actions.RebalanceEnded,
                                                                            time.Elapsed);
                                       
                                        _rebalancingQueued = NotQueued;
                                    });
            }
        }

        private void DecCount()
        {
            Interlocked.Decrement(ref _cacheNodeCount);
        }

        private SpinLock _writeLock = new SpinLock();

        public void Add(TKey key,TValue value)
        {

          
            bool lockTaken = false;
            try
            {
                _writeLock.Enter(ref lockTaken);

                if (Settings.CheckCycles && _root != null)
                    if (!_root.CheckTree(_currentPath, _cacheNodeCount+1))
                        Debugger.Break();

                if (_root == null)
                    _root = new CacheNode<TKey, TValue>(key, value, _maxFixedBranchDepth, _comparer,Channel, DecCount, _frequencyCalc);
                else
                    _root.AddNode(key, value, _maxFixedBranchDepth, _currentPath);

                if (Settings.CheckCycles && _root != null)
                    if (!_root.CheckTree(_currentPath, _cacheNodeCount+1))
                        Debugger.Break();
            }
            finally
            {
                
               if (lockTaken)
                   _writeLock.Exit();
            }
          

           
            Interlocked.Increment(ref _cacheNodeCount);
        }

        

        public IEnumerable<TKey> Keys
        {
            get
            {
                var root = _root;//присвоение для разруливания гонок
                if (root ==null)
                    return new TKey[0];
                return root.GetKeys(_currentPath);
            }
        }

        private CacheNode<TKey, TValue>.Rebalancer CreateRebalancer()
        {
            var count = _cacheNodeCount;
            var rebalancer = new CacheNode<TKey, TValue>.Rebalancer(_root, count, _currentPath,
                                                                    (_currentPath + 1) % CacheNode<TKey, TValue>.PathCount);
            return rebalancer;
        }

      
        internal TValue this[TKey key]
        {
            get { return Find(key); }
        }

        public TValue Find(TKey key)
        {
            if (_root == null)
                return default(TValue);
            int depth;
            var node = _root.FindNode(key, _currentPath, out depth);
            if (!_eqComparer.Equals(node, default(TValue)))
            {
                Interlocked.Add(ref _totalAccessDepth, depth+1);
                Interlocked.Increment(ref _getCount);
                if (_getCount > CheckThreshold)
                {
                    var rate = (float) _totalAccessDepth/_getCount;
                    if (Math.Abs(rate - _expectedGet) > RebalancingDelta)
                        QueueRebalance();

                    _totalAccessDepth >>= 4;
                    _getCount >>= 4;
                }
            }
            return node;
        }

        public void Clear()
        {
            _root = null;
        }

       
        public SecondLevelCache(IComparer<TKey> comparer,Func<TValue,float> frequencyCalc, Action<Action> rebalanceQueue = null)
        {
            _comparer = comparer;           
            _queueRebalance = rebalanceQueue?? (a=>a());
            _frequencyCalc = frequencyCalc;
            _state = new CacheState(this);
           
        }


      
    }
}
