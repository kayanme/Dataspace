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
using Dataspace.Common.Utility.Dictionary.SecondLevelCache;

namespace Dataspace.Common.Utility.Dictionary
{
    public enum RebalancingMode { Heavy, Light, Hybrid }

    internal class SecondLevelCache<TKey,TValue>:IDisposable
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
        private int _maxFixedBranchDepth = 6;

        private readonly Action<Action> _queueRebalance;
        private readonly Func<TValue, float> _frequencyCalc;

        private int _rebalancingQueued;

        private CancellationToken _token;
        private CancellationTokenSource _rebalancerStopper;

        private const int Queued = 1;
        private const int NotQueued = 0;

     
        private readonly CacheController<TKey, TValue> _cacheController;
        private readonly UpdatableElement<TValue> _goneIntensity = new UpdatableElement<TValue>(default(TValue));
        private int _writeTimeout = 50;

        internal event EventHandler NodeGoneEvent;
        /// <summary>
        /// Происходит, когда нода не смогла записаться из-за длительной блокировки кэша.
        /// </summary>
        internal event EventHandler NodeLostEvent;

        internal event EventHandler BranchLengthChangedEvent;

        

        public class CacheState
        {
            private readonly SecondLevelCache<TKey, TValue> _cache;

            internal event EventHandler NodeGone
            {
                add { _cache.NodeGoneEvent += value; }
                remove { _cache.NodeGoneEvent -= value; }
            }

            internal event EventHandler NodeLost
            {
                add { _cache.NodeLostEvent += value; }
                remove { _cache.NodeLostEvent -= value; }
            }

            internal event EventHandler BranchLengthChangedEvent
            {
                add { _cache.BranchLengthChangedEvent += value; }
                remove { _cache.BranchLengthChangedEvent -= value; }
            }

            public float Rate { get { return (float) _cache._totalAccessDepth/_cache._getCount; } }
            public float ExpectedRate { get { return _cache._expectedGet; } }
            public int CurrentPath { get { return _cache._currentPath; } }
            public bool RebalancingQueued { get { return _cache._rebalancingQueued == Queued; } }
            public int MaxFixedBranchDepth { get { return _cache._maxFixedBranchDepth; } internal set { _cache._maxFixedBranchDepth = value;_cache.UpdateBranchDepth(value); } }        
            public int Count { get { return _cache.Count; } }
            public float GoneIntensity { get { return _cache._goneIntensity.GetFrequency(); } }
            public int WriteTimeout { get { return _cache._writeTimeout; } internal set { _cache._writeTimeout = value; } }
            public CacheAdaptationSettings AdaptationSettings { get { return _cache._cacheController.Settings; } }   

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

        internal void QueueRebalance(RebalancingMode mode)
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
                                            var rebalancer = CreateRebalancer(mode);
                                            _expectedGet = rebalancer.Rebalance(_token);                                           
                                            _root = rebalancer.ConstructNewTreeAfterCalculation();
                                            _currentPath = rebalancer.OutPath;
                                            rebalancer.Dispose();
                                        }
                                        finally
                                        {
                                            time.Stop();
                                            Channel.SendMessageAboutOneResource(Guid.Empty, Actions.RebalanceEnded,
                                                                                time.Elapsed);                                         
                                            _rebalancingQueued = NotQueued;                                            
                                            if (lockTaken)
                                                _writeLock.Exit();
                                        }
                                       
                                    });
            }
        }

      

        private void DecCount(DateTime goneTime)
        {
#if DEBUG
            if (NodeGoneEvent != null)
                NodeGoneEvent(this,new EventArgs());
#endif
            _goneIntensity.FixTake(goneTime);
            Interlocked.Decrement(ref _cacheNodeCount);
        }

        private SpinLock _writeLock = new SpinLock();

        private void UpdateBranchDepth(int newBranchDepth)
        {
            bool lockTaken = false;
            try
            {
                _writeLock.TryEnter(_writeTimeout, ref lockTaken);
                if (BranchLengthChangedEvent !=null)
                    BranchLengthChangedEvent(this,new EventArgs());

                if (_root != null)
                {                    
                    var w = Stopwatch.StartNew();
                    _root.UpdateMaxBranchDepth(_currentPath, newBranchDepth);
                    w.Stop();
                    Channel.SendMessageAboutOneResource(Guid.Empty,Actions.BranchChanged,w.Elapsed);                 
                }

            }
            finally
            {

                if (lockTaken)
                    _writeLock.Exit();
            }
        }

        public void Add(TKey key,TValue value)
        {          
            bool lockTaken = false;
            try
            {
                _writeLock.TryEnter(_writeTimeout, ref lockTaken);
                if (lockTaken)
                {
                    if (Settings.CheckCycles && _root != null)
                        if (!_root.CheckTree(_currentPath, _cacheNodeCount + 1))
                            Debugger.Break();

                    if (_root == null)
                        _root = new CacheNode<TKey, TValue>(key, value, _maxFixedBranchDepth, _comparer, Channel,
                                                            DecCount, _frequencyCalc);
                    else
                        _root.AddNode(key, value, _maxFixedBranchDepth, _currentPath);

                    if (Settings.CheckCycles && _root != null)
                        if (!_root.CheckTree(_currentPath, _cacheNodeCount + 1))
                            Debugger.Break();
                    Interlocked.Increment(ref _cacheNodeCount);
                }
                else
                {
#if DEBUG
                    if (NodeLostEvent!=null)
                        NodeLostEvent(this,new EventArgs());
#endif
                }
            }
            finally
            {
                
               if (lockTaken)
                   _writeLock.Exit();
            }
          

           
          
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

        private CacheNode<TKey, TValue>.Rebalancer CreateRebalancer(RebalancingMode mode)
        {
            var count = _cacheNodeCount;
            CacheNode<TKey, TValue>.Rebalancer rebalancer;
            var targetMode = mode;
           
            if (targetMode == RebalancingMode.Heavy)
                rebalancer = new CacheNode<TKey, TValue>.HeavyRebalancer(_root, count, _currentPath,
                                                                    (_currentPath + 1) % CacheNode<TKey, TValue>.PathCount);
            else if (targetMode == RebalancingMode.Light)
            {
                rebalancer = new CacheNode<TKey, TValue>.LightRebalancer(_root, count, _currentPath,
                                                                    (_currentPath + 1) % CacheNode<TKey, TValue>.PathCount);
            }
            else if (targetMode == RebalancingMode.Hybrid)
            {
                rebalancer = new CacheNode<TKey, TValue>.HybridRebalancer(_root, count, _currentPath,
                                                                    (_currentPath + 1) % CacheNode<TKey, TValue>.PathCount,TimeSpan.FromSeconds(10));
            }
            else
            {
                Debugger.Break();
                throw new ArgumentException();
            }
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
                if (_getCount > _cacheController.Settings.CheckThreshold)
                {                    
                    _cacheController.MakeDecision();
                    long gc = _getCount;
                    long tad = _totalAccessDepth;
                    while (_getCount > _cacheController.Settings.CheckThreshold
                       && Interlocked.CompareExchange(ref _getCount,gc>>4,gc) != gc)
                    {
                        gc = _getCount;
                      
                    }
                    Interlocked.CompareExchange(ref _totalAccessDepth, tad >> 4, tad);                                                                       
                }
            }
            return node;
        }

        public void Clear()
        {
            _root = null;
        }

       
        public SecondLevelCache(IComparer<TKey> comparer,
            Func<TValue,float> frequencyCalc,            
            Action<Action> rebalanceQueue = null)
        {
            _comparer = comparer;
            _state = new CacheState(this);
            _cacheController = new CacheController<TKey, TValue>(this);
            _queueRebalance = rebalanceQueue?? (a=>a());
            _frequencyCalc = frequencyCalc;
         
            _rebalancerStopper = new CancellationTokenSource();
            _token = _rebalancerStopper.Token;
        }


        public void Dispose()
        {
          _rebalancerStopper.Cancel();
        }
    }
}
