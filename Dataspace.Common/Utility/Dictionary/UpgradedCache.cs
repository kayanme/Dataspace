using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using Dataspace.Common.Statistics;
using Dataspace.Common.Data;
using Dataspace.Common.Statistics;
using Common.Utility.Dictionary;

namespace Dataspace.Common.Utility.Dictionary
{

    
    internal class UpgradedCache<T, TType, TElementType> :IDisposable where TElementType : UpdatableElement<TType>, new()
    {
        //фиксирует кэш первого уровня до достижения в нем порога записей (нет смысла все время удалять кэш с маленьким набором записей)
        private ConcurrentDictionary<T, TElementType> _lowItemsCountFixing;

        private WeakReference _firstLevelCache;

        private readonly SecondLevelCache<T, TElementType> _secondLevelCache;

        private readonly IStatChannel _channel;

        private readonly StatisticsCollector _collector;

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        private const int MaxItemsInLevel1ToActivateFlush = 50;

        internal class TestMock
        {
            private UpgradedCache<T, TType, TElementType> _cache;

            [ImportingConstructor]
            public TestMock(UpgradedCache<T,TType, TElementType> cache)
            {
                _cache = cache;
            }

            internal ConcurrentDictionary<T, TElementType> FirstLevel { get { return _cache.GetFirstLevelCache(); } }

            internal SecondLevelCache<T, TElementType> SecondLevel { get { return _cache._secondLevelCache; } }

          
        }
                       
        public class CacheState
        {
            private UpgradedCache<T, TType, TElementType> _cache;
            public CacheState(UpgradedCache<T, TType, TElementType> cache)
            {
                _cache = cache;
            }

            public int MaxItemsActivateFlush
            {
                get { return MaxItemsInLevel1ToActivateFlush; }
            }

            public int CurrentItems
            {
                get
                {
                    var flc = _cache._firstLevelCache.Target as ConcurrentDictionary<T, TElementType>;
                    if (flc == null)
                        return 0;
                    else return flc.Count;
                }
            }
        }

        private CacheState _state;



        public UpgradedCache(IComparer<T> comparer = null, IEqualityComparer<T> eqcomparer = null,Action<Action> queueRebalance = null)
        {
            _state = new CacheState(this);
            comparer = comparer ?? Comparer<T>.Default;
            var eqComparer = eqcomparer ?? EqualityComparer<T>.Default;
           
            _secondLevelCache = new SecondLevelCache<T, TElementType>(comparer, k => k.GetFrequency(), queueRebalance ?? QueueRebalance);
            _firstLevelCache = new WeakReference(new ConcurrentDictionary<T, TElementType>(eqComparer));
        }


        protected UpgradedCache(IStatChannel channel, StatisticsCollector collector, IComparer<T> comparer = null, IEqualityComparer<T> eqcomparer = null, Action<Action> queueRebalance = null)
            : this(comparer,eqcomparer,queueRebalance)
        {
           
            _channel = channel;
            _collector = collector;
            _secondLevelCache.Channel = _channel;            
        }

        private void QueueRebalance(Action action)
        {
            if (_collector != null)
                _collector.AddAdditionActionToStatisticsQueue(action);
            else
            {
#if DEBUG
                Debugger.Break();
#endif
                action();
            }
        }
      


        private ConcurrentDictionary<T, TElementType> GetFirstLevelCache()
        {
            Contract.Ensures(Contract.Result<ConcurrentDictionary<T, TElementType>>() != null);
            Contract.Invariant(_firstLevelCache != null);
            ConcurrentDictionary<T, TElementType> flc = null;
            try
            {
                if (_firstLevelCache != null)
                    flc = _firstLevelCache.Target as ConcurrentDictionary<T, TElementType>;
            }
            catch (NullReferenceException)//даже проверка на null не дает гарантии, что после этой проверки _firstLevelCache не обнулится. Проверка исключает часть выбрасываемых исключений
            {

            }
            while (flc == null)//в данной ситуации гарантирована рабочая ссылка по первому проходу цикла.
                //Тем не менее, в целях безопасности (если алгоритм изменится), цикл обеспечивает 100% инициализацию в любых ситуациях и не дороже обычного условного перехода
            {
                flc = new ConcurrentDictionary<T, TElementType>();
                var wr = new WeakReference(flc);
                var oldWr = Interlocked.CompareExchange(ref _firstLevelCache, wr, _firstLevelCache);
                  //вернется та ссыль, которая была до обмена, может там уже и будет словарь, тогда возьмем его. Если его нет - возьмем свой словарь.
                  //wr.Target точно будет не null, т.к. мы ее держим через сильную ссылку flc, можно было бы, в принципе вместе wr.Target присвоить flc
                flc = (oldWr.Target ?? wr.Target) as ConcurrentDictionary<T, TElementType>;
             
                _lowItemsCountFixing = flc;
                if (_channel!=null)
                   _channel.SendMessageAboutOneResource(Guid.Empty,Actions.FirstLevelCacheRecreated);
            }
            return flc;
        }

        private TType TryGetFromSecondLevel(T id, Func<T, TType> value, bool withLock,out bool wasFound)
        {
            try
            {
                if (withLock)
                    _lock.EnterUpgradeableReadLock();

                var element = _secondLevelCache.Find(id);
                if (element != null)
                {
                    wasFound = true;
                    element.FixTake(DateTime.Now);
                    if (!element.NeedUpdate())
                        return element.Element;
                    else
                    {
                        try
                        {
                            if (withLock)
                                _lock.EnterWriteLock();
                            if (element.NeedUpdate())
                            {
                                var startGetTime = DateTime.Now;
                                element.Element = value(id);
                                element.DropUpdate(startGetTime);
                            }
                            return element.Element;
                        }
                        finally
                        {
                            if (withLock)
                                _lock.ExitWriteLock();
                        }
                    }
                }
                else
                {
                    wasFound = false;
                    return default(TType);
                }
               
            }
            finally
            {
                if (withLock)
                    _lock.ExitUpgradeableReadLock();
            }
        }

        private TType GetFromFirstLevel(T id, Func<T, TType> value, bool withLock)
        {
            var flc = GetFirstLevelCache();
            bool added = false;
            var element = flc.GetOrAdd(id, k =>
                                                        {
                                                            added = true;
                                                            var t = new TElementType { Element = value(k) };
                                                            return t;
                                                        });
            Debug.Assert(element!=null,"element!=null");
            if (added)
            {
                if (flc.Count > MaxItemsInLevel1ToActivateFlush && !Settings.NoCacheGarbageChecking)
                    _lowItemsCountFixing = null;
                return element.Element;
            }

            try
            {
                if (withLock)
                    _lock.EnterWriteLock();
                _secondLevelCache.Add(id, element);
                flc.TryRemove(id, out element);
                return element.Element;
            }
            finally
            {
                if (withLock)
                    _lock.ExitWriteLock();
            }
        }

        private TType RetrieveCommon(T id, Func<T, TType> value, bool withLock)
        {
            bool wasFoundInSecondlevel;
            var element = TryGetFromSecondLevel(id, value, withLock,out wasFoundInSecondlevel);
            if (wasFoundInSecondlevel)
                return element;
            try
            {
                return GetFromFirstLevel(id, value, withLock);
            }
            catch (OutOfMemoryException)
            {
                _lowItemsCountFixing = null;
                GC.Collect();
                GC.WaitForFullGCComplete();
                return GetFromFirstLevel(id, value, withLock);             
            }
           
        }

        public TType RetrieveByFunc(T id, Func<T, TType> value)
        {
            Contract.Requires(value!=null);
            Debug.Assert(value != null,"Отсутствует функция получения");
            return RetrieveCommon(id, value, true);
        }

        public void Push(T id, TType value)
        {
             RetrieveCommon(id, k=>value,false);
        }

        public void SetUpdateNecessity(T id,DateTime time)
        {
          
                TElementType element = _secondLevelCache.Find(id);
                if (element != null)
                {                
                    element.SetUpdate(time);
                }
                else
                {
                    GetFirstLevelCache().TryRemove(id, out element);
                }
           
        }

        public void SetUpdateNecessity(T id)
        {
            SetUpdateNecessity(id, DateTime.Now);
        }

        public SecondLevelCache<T,TElementType>.CacheState  SecondLevelCacheState
        {
            get { return _secondLevelCache.State; }
        }

        public CacheState FirstLevelCacheState
        {
            get { return _state; }
        }

        public bool HasActualValue(T id)
        {

            TElementType element = _secondLevelCache.Find(id);
            if (element != null)
            {
                return !element.NeedUpdate();
            }
            else
            {
                GetFirstLevelCache().TryGetValue(id, out element);
                return element != null;
            }
        }

        public void StartUpdates()
        {
            _lock.ExitWriteLock();
        }

        public void StopUpdates()
        {
            _lock.EnterWriteLock();
        }

        public void Clear()
        {
            GetFirstLevelCache().Clear();
            _lock.EnterWriteLock();
            _secondLevelCache.Clear();
            _lock.ExitWriteLock();
        }

        protected IEnumerable<T> Keys { get { return GetFirstLevelCache().Select(k => k.Key).Concat(_secondLevelCache.Keys); } }

        public void Dispose()
        {
            _lock.Dispose();
        }
    }
}
