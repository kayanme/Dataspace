using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
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

    
    internal sealed class DataStore
    {
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

        #region Imports

#pragma warning disable 0649

        [Import]
        private StatisticsCollector _statisticsCollector;

        [Import]
        private ITypedPool TypedPool { get; set; }

        [Import]
        private IGenericPool GenericPool { get; set; }

        [Import]
        private CompositionService _compositionService;

        [Import]
        private QueryStorage _queryStorage;

        [Import]
        private IInterchangeCachier _intCachier;

        [Import]
        private SettingsHolder _settingsHolder;

        [Import]
        private IAnnouncerSubscriptorInt _subscriptor;
#pragma warning restore 0649

        #endregion

        private readonly ResourceGetter _getter;
        private readonly ResourcePoster _poster;
        private readonly ConcurrentDictionary<Guid, ConcurrentBag<UnactualResourceContent>> _dependentUpdates = new ConcurrentDictionary<Guid, ConcurrentBag<UnactualResourceContent>>();
     
        private readonly ResUnactComparer _comp = new ResUnactComparer();

        private bool _noCaching;

        private readonly List<Func<Guid, IEnumerable<UnactualResourceContent>>> _updatesByQueries = new List<Func<Guid, IEnumerable<UnactualResourceContent>>>();
        private readonly List<string> _dependentTypes = new List<string>();
        private readonly List<Action<Guid>> _updateDependenciesByQueries = new List<Action<Guid>>();

        private dynamic _accumulator;

        private readonly TransitionStorage _storage = new TransitionStorage();
     
        private readonly string _name;
        public string Name{get { return _name; }}

        //собственно кэш значений. Для ресурсов и кэш-данных интерфейс хранения один, но реализации должны учитывать специфику объектов -
        //ресурс сериализуем и доступен между машинами, кэш-данные - необязательно несериализуемы и в общем случае недоступны
        private ICachierStorage<Guid> _dataCache;

        public bool IsTracking
        {
            get { return _getter.IsTracking; }
            set { _getter.IsTracking = value; }
        }
        
        internal  DataStore(string name,
                            ResourceGetter getter, 
                            ResourcePoster poster)
        {
            _name = name;
            _getter = getter;
            _poster = poster;
            if (_poster != null)
              _poster.SetStatChannel(_getter.StatChannel);
        }

        private ICachierStorage<Guid> GetStorageForResource()
        {
            var storage = new CurrentCachierStorageNoRef(_getter.StatChannel, _statisticsCollector);
            return storage;
        }

        private ICachierStorage<Guid> GetStorageForCacheData()
        {
            var storage = new CurrentCachierStorageRef(_getter.StatChannel, _statisticsCollector);
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
        /// Обработка запросов при изменении ребенка в запросозависимом кэшировании.
        /// Ребенок выполняет эти запросы тогда, когда был получен не из кэша и добавляет себя в список необходимых обновлений к своему родителю в кэше.
        /// </summary>
        /// <param name="parentGetter">The parent getter.</param>
        internal void ProcessChildQueriedCaching(DataStore parentGetter)
        {
            var parType = GenericPool.GetTypeByName(parentGetter.Name);
            var parentsQuery = _queryStorage.FindQuery(parType, "", new[] { Name });

            _updateDependenciesByQueries.Add(id =>
            {
                var parentKeys = parentsQuery(new object[] { id });
                foreach (var key in parentKeys)
                    parentGetter.AddDependency(key, new UnactualResourceContent { ResourceName = Name, ResourceKey = id });
            });

        }

        internal void SetNoCachePolicy()
        {
            _noCaching = true;
        }


        internal void ProcessDependentCaching(Type cachingPoliticsSender)
        {

            var resName = GenericPool.GetNameByType(cachingPoliticsSender);
            _dependentTypes.Add(resName);
        }

        /// <summary>
        /// Обработка запросов при изменения родителя в запросозависимом кэшировании. Родитель выполняет эти запросы, если он стал неактуален, и помечает как неактуальные всех, связанных с ним детей.
        /// </summary>
        /// <param name="cachingPoliticsSender">Дочерний тип.</param>
        internal void ProcessParentQueriedCaching(Type cachingPoliticsSender)
        {
            var resName = GenericPool.GetNameByType(cachingPoliticsSender);

            var query = _queryStorage.FindQuery(cachingPoliticsSender, "", new UriQuery { { Name, "" } });

            _updatesByQueries.Add(id => query(new UriQuery { { Name, id.ToString() } })
                             .Select(k => new UnactualResourceContent { ResourceName = resName, ResourceKey = k }));

        }

        internal void Initialize(bool isCacheData)
        {
            Debug.Assert(_getter.StatChannel != null);
            Debug.Assert(_statisticsCollector != null);
            _dataCache = isCacheData ? GetStorageForCacheData() : GetStorageForResource();
            _accumulator = _getter.ReturnAccumulator(_dataCache,GetResource);
            IsTracking = _settingsHolder.Settings.AutoSubscription;
            _getter.StatChannel.Register(Name);
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
                Debugger.Break();

            for (int i = 0; i < allUpdates.Count(); i++)
                _intCachier.MarkForUpdate(allUpdates[i]);
            _subscriptor.AnnonunceUnactuality(Name, id);    
            _storage.ClearLastMarked();
        }

        internal object GetResource(Guid id)
        {
            var gotObject = _storage.FindOrReturnNullObject(Name, id);
            if (gotObject != null)
                return gotObject;

            object item;
            var transactedResourceManager = _compositionService.Container.GetExportedValue<TransactedResourceManager>();

            if (Transaction.Current != null)
            {
                var dataItem = transactedResourceManager.GetResource(new UnactualResourceContent { ResourceKey = id, ResourceName = Name });
                if (dataItem.Content != null)
                    return dataItem.Resource;
            }

            if (_noCaching || Transaction.Current != null && Transaction.Current.IsolationLevel == IsolationLevel.ReadUncommitted)
                item = _getter.GetItem(id);
            else
            {

                bool cameFromoutside = false;
                var t = Stopwatch.StartNew();
                item = _dataCache.RetrieveByFunc(id, i =>
                {
                    cameFromoutside = true;
                    return _getter.GetItem(i);
                });
                t.Stop();
                _storage.PutObject(Name, id, item);

                if (cameFromoutside)
                {
                    _updateDependenciesByQueries.ForEach(k => k(id));
                    _getter.StatChannel.SendMessageAboutOneResource(id, Actions.ExternalGet, t.Elapsed);
                }
                else
                {
                    _getter.StatChannel.SendMessageAboutOneResource(id, Actions.CacheGet, t.Elapsed);
                }
                _storage.ClearObject();


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

        private void WriteResource(Guid key, object resource)
        {
            try
            {
                _poster.WriteResourceRegardlessofTransaction(key, resource);
                _intCachier.MarkForUpdate(new UnactualResourceContent {ResourceName = Name, ResourceKey = key});
            }
            catch (Exception ex)
            {
                throw new PostException(Name, ex);
            }

        }

        internal void PostResource(Guid id,object resource)
        {
            var poster = resource != null ? new Action<Guid, object>(WriteResource)
                                          : ((key,_) => DeleteResource(key));
            var transactedResourceManager = _compositionService.Container.GetExportedValue<TransactedResourceManager>();

            transactedResourceManager.AddResourceToSend(
                   new UnactualResourceContent { ResourceKey = id, ResourceName = Name },
                   resource,
                   poster);
        }

        private void DeleteResource(Guid key)
        {            
                try
                {
                    _poster.DeleteResourceRegardlessofTransaction(key);
                    _intCachier.MarkForUpdate(new UnactualResourceContent {ResourceName = Name, ResourceKey = key});
                }
                catch (Exception ex)
                {
                    throw new PostException(Name, ex);
                }        
        }

    }
}
