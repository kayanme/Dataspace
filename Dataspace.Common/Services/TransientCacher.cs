using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using Dataspace.Common.Announcements;
using Dataspace.Common;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Projections.Storages;
using Dataspace.Common.Services;
using SecurityManager = Dataspace.Common.Security.SecurityManager;

[assembly: InternalsVisibleTo("TestHelper")]
[assembly: InternalsVisibleTo("Common.TestsMocks")]

namespace Common
{
    /// <summary>
    /// Реализация кэшера ресурсов.
    /// </summary>
    [Export(typeof(ITypedPool))]
    [Export(typeof(IGenericPool))]
    [Export(typeof(IInterchangeCachier))]
    [Export(typeof(IInitialize))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TransientCacherImpl : ITypedPool,IGenericPool,IInterchangeCachier,IInitialize,IDisposable
    {

#pragma warning disable 0649
                        
        [Import(AllowRecomposition = false)]
        private SecurityManager _securityManager;
       
        [Import(AllowRecomposition = false)]
        private QueryStorage _storage;

        [Import(AllowRecomposition = false)]
        private ICacheServicing _service;

        [Import(AllowRecomposition = false)]
        private SettingsHolder _settingsHolder;

        [Import(AllowRecomposition = false)]
        private Initializer _initializer;    

        [Import(AllowRecomposition = false)]
        private IAnnouncerSubscriptorInt _subscriptor;

        [Import]
        private RegistrationStorage _registrationStorage;
#pragma warning restore 0649

       
     
        Dictionary<Type,DataStore> _stores = new Dictionary<Type, DataStore>();
                                            

        
                 
        private void TypeCheck(Type type)
        {

            Debug.Assert(_registrationStorage.IsResourceRegistered(type) == true, "Ресурс не зарегистрирован: " + type.Name);
            if (!_registrationStorage.IsResourceRegistered(type))
                throw new ArgumentException("Ресурс не зарегистрирован: "+type.Name); 
        }

        private void CheckInitialization()
        {
            if (!_service.IsInitialized)
                _service.Initialize();
        }                                                    

        /// <summary>
        /// Получение ресурса по типу.
        /// </summary>
        /// <param name="type">Тип.</param>
        /// <param name="id">Ключ.</param>
        /// <returns>Ресурс</returns>
        private object GetResourceCommon(Type type, Guid id)
        {
            TypeCheck(type);
            var token = _securityManager.GetToken(type, id);
            if (token.CanRead)
               return _stores[type].GetResource(id);
            else
            {
                return null;
            }
        }

      
        /// <summary>
        /// Отложенное получение ресурса по типу.
        /// </summary>
        /// <param name="type">Тип.</param>
        /// <param name="id">Ключ.</param>
        /// <returns>Ресурс</returns>
        private Func<object> GetResourceDeferred(Type type, Guid id)
        {
            Contract.Requires(type != null);
            Contract.Ensures(Contract.Result<Func<object>>() != null);
            TypeCheck(type);
            var token = _securityManager.GetTokenDeferred(type, id);
            return _stores[type].GetResourceDeferred(id, token);           
        }

        /// <summary>
        /// Позволяет делаеть отложенную загрузку ресурса. Отложенная загрузка позволяет накапливать серии данных,
        /// получаемые только в требуемый момент.
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="id">Ключ.</param>
        /// <returns>
        /// Отложенное значение
        /// </returns>
        public Lazy<T> GetLater<T>(Guid id) where T : class
        {
            CheckInitialization();          
            var resFunc = GetResourceDeferred(typeof(T), id);
            return new Lazy<T>(() => resFunc() as T);
        }

        /// <summary>
        /// Отложенно получает ресурс по имени.
        /// </summary>
        /// <param name="name">Имя.</param>
        /// <param name="id">Ключ.</param>
        /// <returns>
        /// Обертка для отложенного получения ресурса.
        /// </returns>
        public Lazy<object> GetLater(string name,Guid id)
        {
            CheckInitialization();
            if (!_registrationStorage.IsResourceRegistered(name))
                throw new ArgumentException("Ресурс не зарегистрирован ");
            var type =_registrationStorage[name].ResourceType;
            return new Lazy<object>(GetResourceDeferred(type, id));
        }

        


        /// <summary>
        /// Получает имя ресурса по типу.
        /// </summary>
        /// <param name="type">Тип.</param>
        /// <returns>
        /// Имя ресурса
        /// </returns>
        public string GetNameByType(Type type)
        {
            CheckInitialization();
            try
            {
                return _registrationStorage[type].ResourceName;
            }
            catch (NullReferenceException)
            {
                
                throw new KeyNotFoundException();
            }
            
        }

        /// <summary>
        /// Получение ресурса по первичному ключу.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <param name="id">Ключ.</param>
        /// <returns>
        /// Ресурс; null, если ресурс отсутсвует
        /// </returns>
        public T Get<T>(Guid id) where T : class
        {
            CheckInitialization();                 
            return GetResourceCommon(typeof(T), id) as T;

        }

        /// <summary>
        /// Получение ресурса по имени и ключу.
        /// </summary>
        /// <param name="name">Имя.</param>
        /// <param name="id">Ключ.</param>
        /// <returns>
        /// Ресурс
        /// </returns>
        public object Get(string name, Guid id)
        {          
            Debug.Assert(!string.IsNullOrEmpty(name));
            CheckInitialization();
            if (!_registrationStorage.IsResourceRegistered(name))
                throw new ArgumentException("Ресурс не зарегистрирован ");
            var type = _registrationStorage[name].ResourceType;
            return GetResourceCommon(type, id);
        }


        /// <summary>
        /// Получение ключей ресурсов по запросу.
        /// </summary>
        /// <param name="nmspace">неймеспейс </param>
        /// <param name="query">Запрос.</param>
        /// <param name="type">Тип </param>
        /// <returns>
        /// Ключи ресурсов.
        /// </returns>
        private IEnumerable<Guid> GetResourcesCommon(Type type, string nmspace, UriQuery query)
        {
            Contract.Requires(query != null);            
            Contract.Ensures(Contract.Result<IEnumerable<Guid>>() != null);
            TypeCheck(type);
            
            var foundQuery =_storage.FindQuery(type, nmspace, query);
            return foundQuery(query);

        }

        /// <summary>
        /// Получение ключей ресурсов по запросу.
        /// </summary>
        /// <typeparam name="T">Тип.</typeparam>
        /// <param name="query">Запрос.</param>
        /// <returns>
        /// Ключи ресурсов.
        /// </returns>
        public IEnumerable<Guid> Query<T>(UriQuery query, string nmspace = "") where T : class
        {          
            CheckInitialization();
            return GetResourcesCommon(typeof (T),nmspace, query);

        }

        /// <summary>
        /// Получение ключей ресурсов по запросу.
        /// </summary>
        /// <param name="name">Имя ресурса.</param>
        /// <param name="query">Запрос.</param>
        /// <param name="nmspace">Область запросов.</param>
        /// <returns>Ключи ресурсов.</returns>
        public IEnumerable<Guid> Query(string name, UriQuery query, string nmspace = "") 
        {
            CheckInitialization();
            var type = _registrationStorage[name].ResourceType;
            return GetResourcesCommon(type, nmspace,query);

        }

        public void Dispose()
        {
            _subscriptor.Dispose();
           _registrationStorage.FlushRegistraions();
            foreach (var dataStore in _stores)
            {
                dataStore.Value.Dispose();
            }
        }

        private IEnumerable<Type> GetResourceTypeForCurrentBaseType(Type parentType, Type childType)
        {
            return childType.Construct(k => k != parentType, k => k.BaseType)
                                         .Where(_registrationStorage.IsResourceRegistered);
        }

        /// <summary>
        /// Запись ресурса по типу.
        /// </summary>
        /// <param name="type">Тип.</param>
        /// <param name="id">Ключ.</param>
        /// <param name="resource">Ресурс.</param>
        private void PostResource(Type type,Guid id,object resource)
        {
            CheckInitialization();
            TypeCheck(type);

            if (_settingsHolder.Settings.AwareOfInheritance)
            {
                if (resource != null && resource.GetType() != type)
                {
                    var possiblyNewtypes = GetResourceTypeForCurrentBaseType(type, resource.GetType());
                    var firstWriteAvailableType =
                        possiblyNewtypes.FirstOrDefault(t => _securityManager.GetToken(t, id).CanWrite);
                    type = firstWriteAvailableType ?? type;
                }
            }

            var token = _securityManager.GetToken(type, id);
            if (!token.CanWrite)
                throw new SecurityException("Post denied");

            var store = _stores[type];       
            store.PostResource(id, resource);                      
        }

        /// <summary>
        /// Записывает ресурс.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <param name="id">Ключ ресурса.</param>
        /// <param name="resource">Ресурс.</param>
        public void Post<T>(Guid id, T resource)
        {
            var type = typeof (T);
            PostResource(type,id,resource);
        }


        /// <summary>
        /// Posts the name of the resource by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="id">The id.</param>
        /// <param name="resource">The resource.</param>
        public void Post(string name, Guid id, object resource)
        {
            CheckInitialization();
            Debug.Assert(!string.IsNullOrEmpty(name));
            var type = _registrationStorage[name].ResourceType;
            PostResource(type, id, resource);
        }


        /// <summary>
        /// Получение ключей ресурсов по запросу.
        /// </summary>
        /// <typeparam name="T">Тип.</typeparam>
        /// <param name="query">Запрос.</param>
        /// <returns>
        /// Ресурс.
        /// </returns>
        public IEnumerable<Guid> Query<T>(string query) where T : class
        {
            CheckInitialization();
            return Query<T>(new UriQuery(query));
        }

        private class DynamicQuery : DynamicObject
        {

            private object _lock = new object();

            public readonly List<string> Names = new List<string>();
            public readonly List<object> Args = new List<object>();

            public object this[string name]
            {
                get { 
                    var index = Names.FindIndex(k => k == name);
                    if (index == -1)
                        throw new ArgumentException("No such parameter in query");
                    return Args[index];
                }
            }

            public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
            {
                lock (_lock)
                {
                    Names.AddRange(binder.CallInfo.ArgumentNames.ToArray());
                    Args.AddRange(args);
                }
                result = this;
                return true;
            }

            /// <summary>
            /// Provides the implementation for operations that set a value by index. Classes derived from the <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for operations that access objects by a specified index.
            /// </summary>
            /// <param name="binder">Provides information about the operation.</param>
            /// <param name="indexes">The indexes that are used in the operation. For example, for the sampleObject[3] = 10 operation in C# (sampleObject(3) = 10 in Visual Basic), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject" /> class, <paramref name="indexes[0]" /> is equal to 3.</param>
            /// <param name="value">The value to set to the object that has the specified index. For example, for the sampleObject[3] = 10 operation in C# (sampleObject(3) = 10 in Visual Basic), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject" /> class, <paramref name="value" /> is equal to 10.</param>
            /// <returns>
            /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.
            /// </returns>
            public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
            {
                Debug.Assert(indexes.Length>0);
                Debug.Assert(indexes[0] is string);
                lock(_lock)
                {
                    Names.Add((string) indexes[0]);
                    Args.Add(value);
                }
                return true;
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                lock (_lock)
                {
                    Names.Add(binder.Name);
                    Args.Add(value);
                }
                return true;
            }
        }

        private IEnumerable<Guid> FindCommon(Type type,object query, string namespc = "")
        {
            var t = query as DynamicQuery;            
            if (t == null)
                throw new ArgumentException("Запрос должен быть создан с использованием свойства Spec");
            return _storage.FindQuery(type, namespc, t.Names.ToArray(), t.Args.Select(k=>k.GetType()).ToArray())(t.Args.ToArray());          
        }

        private IEnumerable<KeyValuePair<Guid,IEnumerable<Guid>>> FindAndGroupCommon(Type type,object query, string resourceToGroup, string namespc = "")
        {
            var t = query as DynamicQuery;
            if (t == null)
                throw new ArgumentException("Запрос должен быть создан с использованием свойства Spec");
            var foundQuery = _storage.FindQueryWithGrouping(resourceToGroup, type, namespc, t.Names.ToArray(),
                                                            t.Args.Select(k=>t.GetType()).ToArray());
            var group = t[resourceToGroup];
            IEnumerable<Guid> resPack;
            if (group is IEnumerable<Guid>)
                resPack = group as IEnumerable<Guid>;
            else if (group is Guid)
            {
                resPack = new[] {(Guid) group};
            }
            else throw new ArgumentException("Grouping resource parameter is neither a Guid or IEnumerable<Guid>");
            var res =  foundQuery(resPack, t.Names.Where(k2=>resourceToGroup != k2).Select(k => t[k]).ToArray());

            return res;
        }



        public IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>> FindAndGroup<T>(object query, string resourceToGroup, string namespc = "")
        {
            CheckInitialization();
            return FindAndGroupCommon(typeof (T), query, resourceToGroup, namespc);
        }

        public dynamic Spec { get { return new DynamicQuery(); } }

        public IEnumerable<Guid> Find<T>(object query, string namespc = "")
        {            
            CheckInitialization();
            return FindCommon(typeof (T),query, namespc);
        }

        public Type GetTypeByName(string name)
        {
            CheckInitialization();
            var type = _registrationStorage[name].ResourceType;
            return type;
        }


        public IEnumerable<Type> GetResourceTypes()
        {
            CheckInitialization();
            return _registrationStorage.GetRegisteredTypes();
        }


        public bool IsRegistered(string name)
        {
            CheckInitialization();
            return _registrationStorage.IsResourceRegistered(name);
        }

        public void Push<T>(Guid key, T resource)
        {
            CheckInitialization();
            var store = _stores[typeof (T)];
            store.PushInCache(key,resource);
        }

        public void MarkSubscriptionForResource(Type t)
        {
            CheckInitialization();
            _stores[t].IsTracking = _settingsHolder.Settings.AutoSubscription || true;
        }

        public void UnmarkSubscriptionForResource(Type t)
        {
            CheckInitialization();
            _stores[t].IsTracking = _settingsHolder.Settings.AutoSubscription || false;
        }

        public void SetAsUnactual(string name,Guid id)
        {
            CheckInitialization();
            var type = _registrationStorage[name].ResourceType;
            _stores[type].MarkAsUnactual(id);
        }

        public void SetAsUnactual<T>(Guid id)
        {
            CheckInitialization();
            var type = typeof(T);
            _stores[type].MarkAsUnactual(id);
        }

     

        public void MarkForUpdate(UnactualResourceContent res)
        {
            var resToken = _registrationStorage[res.ResourceName];
            if (resToken == null)
                throw new ArgumentException(string.Format("Resource {0} is unregistered",res.ResourceName));
            var type = resToken.ResourceType;
            _stores[type].MarkAsUnactual(res.ResourceKey);
        }

        public void MarkForSecurityUpdate(SecurityUpdate res)
        {
            Contract.Requires(res != null);
            var resToken = _registrationStorage[res.ResourceName];
            if (resToken == null)
                throw new ArgumentException(string.Format("Resource {0} is unregistered", res.ResourceName));
            var type = resToken.ResourceType;
            if (res.UpdateAll)            
                _securityManager.UpdateSecurity(type);            
            else  
                _securityManager.UpdateSecurity(type,res.ResourceKey);
        }


        public void OnNext(ResourceDescription value)
        {
            Contract.Assert(value is SecurityUpdate || value is UnactualResourceContent);
            CheckInitialization();
            if (value is UnactualResourceContent)
            {
                MarkForUpdate(value as UnactualResourceContent);
                _subscriptor.UnsubscribeForResourceChangePropagation(value.ResourceName, value.ResourceKey);
            }
            else
            {
                MarkForSecurityUpdate(value as SecurityUpdate);
            }
        }

        public void OnError(Exception error)
        {
            
        }

        public void OnCompleted()
        {
            
        }


        public void CachePanic()
        {
            foreach(var cache in _stores.Values)
                cache.FlushCache();
        }

        public int Order { get { return 10; } }
        public void Initialize()
        {
            _stores = _initializer.InitializeAndReturnStores();
        }
    }
}
