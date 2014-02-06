using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Transactions;
using Dataspace.Common.Announcements;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Statistics;
using Dataspace.Common.Data;
using Dataspace.Common.Exceptions;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Security;
using Dataspace.Common.Transactions;
using Dataspace.Common.Statistics;

namespace Dataspace.Common.Services
{

    
    internal sealed class DataStore:IDisposable 
    {
        private readonly ResUnactComparer _comp = new ResUnactComparer();

        private readonly List<string> _dependentTypes = new List<string>();
        private readonly List<string> _dependentQueriedTypes = new List<string>();

        private readonly ConcurrentDictionary<Guid, ConcurrentBag<UnactualResourceContent>> _dependentUpdates =
            new ConcurrentDictionary<Guid, ConcurrentBag<UnactualResourceContent>>();

        private readonly ResourceGetter _getter;
        private readonly string _name;
        private readonly ResourcePoster _poster;
        private readonly DataStoreServicesPackage _services;
        private readonly TransitionStorage _storage = new TransitionStorage();

        private readonly List<Action<Guid>> _updateDependenciesByQueries = new List<Action<Guid>>();

        private readonly List<Func<Guid, IEnumerable<UnactualResourceContent>>> _updatesByQueries =
            new List<Func<Guid, IEnumerable<UnactualResourceContent>>>();

        private IAccumulator<Guid, object> _accumulator;

        private event Action<UnactualResourceContent> _unactualityAnnounce;
        private int _pendingUpdatesCount;
        private readonly IDisposable _unactualitiesToken;

        //собственно кэш значений. Для ресурсов и кэш-данных интерфейс хранения один, но реализации должны учитывать специфику объектов -
        //ресурс сериализуем и доступен между машинами, кэш-данные - необязательно несериализуемы и в общем случае недоступны
        private ICachierStorage<Guid> _dataCache;
        private bool _noCaching;

        internal DataStore(string name,
                           ResourceGetter getter,
                           ResourcePoster poster,
                           DataStoreServicesPackage services)
        {
            Debug.Assert(services != null, "services != null");
            _services = services;
            _name = name;
            _getter = getter;
            _poster = poster;
            if (_poster != null)
                _poster.SetStatChannel(_getter.StatChannel);

            _unactualitiesToken = Observable.FromEvent<UnactualResourceContent>
                (h => _unactualityAnnounce += h,
                 h => _unactualityAnnounce -= h)
                                            .ObserveOn(NewThreadScheduler.Default)
                                            .Subscribe(
                                                a =>
                                                {
                                                    try
                                                    {
                                                        _services._intCachier.MarkForUpdate(a);
                                                    }
                                                    finally
                                                    {
                                                        UnlockForReadingThisAndDependent();
                                                    }
                                                });
        }


        internal void LockForReadingThisAndDependent(List<string> alreadyProcessed = null)
        {
            alreadyProcessed = alreadyProcessed ?? new List<string>();
            Interlocked.Increment(ref _pendingUpdatesCount);
            alreadyProcessed.Add(Name);
            _services._intCachier.LockCaches(_dependentTypes.Concat(_dependentQueriedTypes), alreadyProcessed);
        }

        internal void UnlockForReadingThisAndDependent(List<string> alreadyProcessed = null)
        {
            alreadyProcessed = alreadyProcessed ?? new List<string>();
            alreadyProcessed.Add(Name);
            _services._intCachier.UnlockCaches(_dependentTypes.Concat(_dependentQueriedTypes), alreadyProcessed);

            Interlocked.Decrement(ref _pendingUpdatesCount);
        }



        public string Name
        {
            get { return _name; }
        }

        public bool IsTracking
        {
            get { return _getter.IsTracking; }
            set { _getter.IsTracking = value; }
        }

        private ICachierStorage<Guid> GetStorageForResource(StatisticsCollector statisticsCollector, bool keepAllItems)
        {
            var storage = new CurrentCachierStorageNoRef(_getter.StatChannel, statisticsCollector,
                                                         new CachierStorageSettings { KeepAllItems = keepAllItems });
            return storage;
        }

        private ICachierStorage<Guid> GetStorageForCacheData(StatisticsCollector statisticsCollector, bool keepAllItems)
        {
            var storage = new CurrentCachierStorageRef(_getter.StatChannel, statisticsCollector,
                                                         new CachierStorageSettings { KeepAllItems = keepAllItems });
            return storage;
        }

        private void AddDependency(Guid id, UnactualResourceContent unactualResource)
        {
            _dependentUpdates.AddOrUpdate(id,
                                          new ConcurrentBag<UnactualResourceContent> { unactualResource },
                                          (i, b) =>
                                          {
                                              b.Add(unactualResource);
                                              return b;
                                          });
        }


        /// <summary>
        ///     Обработка запросов при изменении ребенка в запросозависимом кэшировании.
        ///     Ребенок выполняет эти запросы тогда, когда был получен не из кэша и добавляет себя в список необходимых обновлений к своему родителю в кэше.
        /// </summary>
        /// <param name="parentGetter">The parent getter.</param>
        internal void ProcessChildQueriedCaching(DataStore parentGetter)
        {
            var parType = _services.GenericPool.GetTypeByName(parentGetter.Name);
            var parentsQuery = _services._queryStorage.FindQuery(parType, "", new[] { Name },
                                                                         new[] { typeof(Guid) });

            _updateDependenciesByQueries.Add(id =>
            {
                var parentKeys = parentsQuery(new object[] { id });
                foreach (var key in parentKeys)
                    parentGetter.AddDependency(key,
                                               new UnactualResourceContent
                                               {
                                                   ResourceName
                                                       =
                                                       Name,
                                                   ResourceKey
                                                       =
                                                       id
                                               });
            });
        }

        internal void SetNoCachePolicy()
        {
            _noCaching = true;
        }


        internal void ProcessDependentCaching(Type cachingPoliticsSender)
        {
            var resName = _services.GenericPool.GetNameByType(cachingPoliticsSender);
            _dependentTypes.Add(resName);
        }

        /// <summary>
        ///     Обработка запросов при изменения родителя в запросозависимом кэшировании. Родитель выполняет эти запросы, если он стал неактуален, и помечает как неактуальные всех, связанных с ним детей.
        /// </summary>
        /// <param name="cachingPoliticsSender">Дочерний тип.</param>
        internal void ProcessParentQueriedCaching(Type cachingPoliticsSender)
        {
            string resName = _services.GenericPool.GetNameByType(cachingPoliticsSender);

            var query = _services._queryStorage.FindQuery(cachingPoliticsSender, "",
                                                                                        new UriQuery { { Name, "" } });
            _dependentQueriedTypes.Add(resName);
            _updatesByQueries.Add(id => query(new UriQuery { { Name, id.ToString() } })
                                            .Select(
                                                k =>
                                                new UnactualResourceContent { ResourceName = resName, ResourceKey = k }));
        }

        internal void Initialize(bool isCacheData, bool keepItems)
        {
            Debug.Assert(_getter.StatChannel != null);
            Debug.Assert(_services._statisticsCollector != null);
            _dataCache = isCacheData
                             ? GetStorageForCacheData(_services._statisticsCollector, keepItems)
                             : GetStorageForResource(_services._statisticsCollector, keepItems);

            _accumulator = _getter.ReturnAccumulator(_dataCache, GetResource);
            IsTracking = _services._settingsHolder.Settings.AutoSubscription;
            _getter.StatChannel.Register(Name);
        }

        internal void PushInCache(Guid key, object resource)
        {
            _dataCache.Push(key, resource);
            if (Transaction.Current != null)
            {
                _services._resourceManager.AddResourceToCurrentTransaction(
                    new UnactualResourceContent { ResourceKey = key, ResourceName = Name }, resource);
            }
        }

        internal void MarkAsUnactual(Guid id)
        {
            UnactualResourceContent[] allUpdates;
            ConcurrentBag<UnactualResourceContent> bagUpdates;
            if (_storage.IsMarkedObject(Name, id))
                return;
            _storage.PutMarkedObject(Name, id);

            if (!_dependentUpdates.TryGetValue(id, out bagUpdates))
                allUpdates = new UnactualResourceContent[0];
            else
            {
                var list = new List<UnactualResourceContent>();
                while (!bagUpdates.IsEmpty)
                {
                    UnactualResourceContent t;
                    if (bagUpdates.TryTake(out t))
                        list.Add(t);
                }
                allUpdates = list.ToArray();
            }


            // ReSharper disable ImplicitlyCapturedClosure
            var queryUpdates = _updatesByQueries.SelectMany(k => k(id));
            // ReSharper restore ImplicitlyCapturedClosure

            allUpdates = queryUpdates
                .Concat(allUpdates)
                .Concat(_dependentTypes.Select(k => new UnactualResourceContent { ResourceKey = id, ResourceName = k }))
                .Distinct(_comp)
                .ToArray();

            _dataCache.SetUpdateNecessity(id);

            _getter.StatChannel.SendMessageAboutOneResource(id, Actions.BecameUnactual);
            if (allUpdates.Any(k => k.ResourceName == Name && k.ResourceKey == id))
                if (Debugger.IsAttached) Debugger.Break();

            for (int i = 0; i < allUpdates.Count(); i++)
                _services._intCachier.MarkForUpdate(allUpdates[i]);
            _services._subscriptor.AnnonunceUnactuality(Name, id);
            _storage.ClearLastMarked();
        }

        private object GetFromTransactionManager(Guid id)
        {
            var dataItem =
                   _services._resourceManager.GetResource(new UnactualResourceContent
                   {
                       ResourceKey = id,
                       ResourceName = Name
                   });
            if (dataItem != null)
                return dataItem.Resource;
            return null;
        }

        private object GetFromCacheOrOutside(Guid id)
        {
            bool cameFromoutside = false;
            var t = Stopwatch.StartNew();
            var item = _dataCache.RetrieveByFunc(id, i =>
            {
                cameFromoutside = true;
                return _getter.GetItem(i);
            });
            t.Stop();
            _storage.PutObject(Name, id, item);

            if (cameFromoutside)
            {
                _updateDependenciesByQueries.ForEach(k => k(id));
                //сообщение о том, что ресурс пришел снаружи, приходит из геттера                    
            }
            else
            {
                _getter.StatChannel.SendMessageAboutOneResource(id, Actions.CacheGet, t.Elapsed);
            }
            _storage.ClearObject();
            return item;
        }

        internal object GetResource(Guid id)
        {
            if (_storage.IsTransitionEmpty)
                SpinWait.SpinUntil(() => _pendingUpdatesCount == 0);
            object gotObject = _storage.FindOrReturnNullObject(Name, id);
            if (gotObject != null)
                return gotObject;

            object item;

            if (Transaction.Current != null)
            {
                var dataItem = GetFromTransactionManager(id);
                if (dataItem != null)
                    return dataItem;
            }

            if (_noCaching ||
                Transaction.Current != null && Transaction.Current.IsolationLevel == IsolationLevel.ReadUncommitted)
                item = _getter.GetItem(id);
            else
            {

                item = GetFromCacheOrOutside(id);
            }

            return item;


        }

        internal Func<object> GetResourceDeferred(Guid id, Lazy<SecurityToken> token)
        {
            Contract.Requires(token != null);
            Contract.Ensures(Contract.Result<Func<object>>() != null);
            var gotValue = _accumulator.GetValue(id);
            Func<object> factory = () =>
            {
                if (token.Value.CanRead)
                {

                    SpinWait.SpinUntil(() => _pendingUpdatesCount == 0);
                    var res = gotValue();
                    return res;
                }
                return null;
            };
            return factory;
        }

        internal void FlushCache()
        {
            _dataCache.Clear();
        }

        private void DeleteResource(Guid key)
        {
            try
            {
                LockForReadingThisAndDependent();
                _poster.DeleteResourceRegardlessofTransaction(key);
                _services._intCachier.MarkForUpdate(new UnactualResourceContent { ResourceName = Name, ResourceKey = key });
            }
            catch (Exception ex)
            {
                throw new PostException(Name, ex);
            }
            finally
            {
                UnlockForReadingThisAndDependent();
            }
        }

        private void WriteResource(Guid key, object resource)
        {
            Debug.Assert(resource != null, "resource!=null");
            try
            {

                if (_poster == null)
                    throw new InvalidOperationException("There is no poster for resource " + _name);
                _poster.WriteResourceRegardlessofTransaction(key, resource);
                SendUpdate(key);

            }
            catch (Exception ex)
            {
                throw new PostException(Name, ex);
            }
        }

        private void SendUpdate(Guid key)
        {
            var update = new UnactualResourceContent { ResourceName = Name, ResourceKey = key };
            if (_unactualityAnnounce != null && _services._settingsHolder.Settings.AsyncronousUpdates)
            {
                LockForReadingThisAndDependent();
                _unactualityAnnounce(update);
            }
            else
            {
                try
                {
                    LockForReadingThisAndDependent();
                    _services._intCachier.MarkForUpdate(update);
                }
                finally
                {
                    UnlockForReadingThisAndDependent();
                }

            }
        }

        internal void PostResource(Guid id, object resource)
        {

            var poster = new Action<Guid, object>((k, o) =>
            {
                if (o != null)
                    WriteResource(k, o);
                else
                    DeleteResource(k);
            });

            var updateSender = new Action(() =>
            {
                SendUpdate(id);
            });

            _services._resourceManager.AddResourceToSend(
                new UnactualResourceContent { ResourceKey = id, ResourceName = Name },
                resource,
                poster, updateSender);
        }





        private class ResUnactComparer : IEqualityComparer<UnactualResourceContent>
        {
            public bool Equals(UnactualResourceContent x, UnactualResourceContent y)
            {
                return (x == null && y == null)
                       || (x != null && y != null && x.ResourceKey == y.ResourceKey && x.ResourceName == y.ResourceName);
            }

            public int GetHashCode(UnactualResourceContent obj)
            {
                return obj.GetHashCode();
            }
        }

        public void Dispose()
        {
            _unactualitiesToken.Dispose();
        }
    }
}
