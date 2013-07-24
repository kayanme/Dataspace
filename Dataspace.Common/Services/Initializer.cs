using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Dataspace.Common;
using Dataspace.Common.Attributes;
using Dataspace.Common.Attributes.CachingPolicies;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Projections.Storages;
using Dataspace.Common.Security;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.ServiceResources;

namespace Dataspace.Common.Services
{
    [Export]
    internal sealed class Initializer
    {

        #region Imports

#pragma warning disable 0649
        //собственно, они (точнее создаватели по умолчанию)      
        [Import(AllowDefault = true,AllowRecomposition = true)]
        private IResourceGetterFactory _getterFactory;

        [Import(AllowDefault = true, AllowRecomposition = true)]
        private IResourcePosterFactory _posterFactory;

        [Import(AllowDefault = true)]
        private CompositionService _container;
              
        [Import(AllowRecomposition = false)]
        private SecurityManager _securityManager;
        
        [Import(AllowRecomposition = false)]
        private DefaultFabrica _fabrica;
       
        [Import(AllowRecomposition = false, RequiredCreationPolicy = CreationPolicy.Shared)]
        private SettingsHolder _settingsHolder;

        [Import(AllowRecomposition = false, RequiredCreationPolicy = CreationPolicy.Shared)]
        private RegistrationStorage _registrationStorage;

        [ImportMany(typeof (ResourceGetter))] 
        private IEnumerable<Lazy<ResourceGetter,ActivationSwitchAttribute>> getters;

        [ImportMany(typeof (ResourcePoster))] 
        private IEnumerable<Lazy<ResourcePoster,ActivationSwitchAttribute>> writers;

        [Import(RequiredCreationPolicy = CreationPolicy.Shared)]         
        private DataStore.DataStoreServicesPackage _servicesPackage;
#pragma warning restore 0649

        #endregion

        private Dictionary<Type, ResourceGetter> _providers;

        private Dictionary<Type, ResourcePoster> _posters;

       
     

        private Dictionary<Type, DataStore> _stores = new Dictionary<Type, DataStore>();



        private ResourceGetter GetDefaultGetter(Type type)
        {
            return  _fabrica.DefaultCreator<ResourceGetter>("получатель", "CreateGetter", type, _getterFactory);
        }

        private ResourcePoster GetDefaultWriter(Type type)
        {
            return  _fabrica.DefaultCreator<ResourcePoster>("постер", "CreateWriter", type, _posterFactory);
        }

       

       
        internal void PrepareRegistration(Registration registration)
        {
         
            Debug.Assert(
              getters.All(
                  k => k.GetType().BaseType != null && k.GetType().BaseType.GetGenericArguments().Length == 1));

            Func<ResourceGetter, Type> provType =
                k => k.GetType().BaseType.GetGenericArguments().First();

            Func<ResourcePoster, Type> wrType =
               k =>
                   {
                       var t = k.GetType().Construct(k2 => k2.BaseType != typeof (ResourcePoster), k2 => k2.BaseType);
                       return t.Last().BaseType.GetGenericArguments().First();
                   };
            //все известны кастомные получатели-писатели заполняем сразу. Потом добавим по умолчанию кому надо.
        
            _providers = getters.Where(k =>_settingsHolder.Settings.ActivationSwitchMatch(k.Metadata,_settingsHolder.Provider))
                                .GroupBy(k => provType(k.Value))
                                .Select(k=>k.First().Value)
                                .ToDictionary(provType);

            _posters = writers.Where(k => _settingsHolder.Settings.ActivationSwitchMatch(k.Metadata, _settingsHolder.Provider))
                                .GroupBy(k => wrType(k.Value))
                                .Select(k => k.First().Value)
                                .ToDictionary(wrType);

            var type = registration.ResourceType;          
            var provider = _providers.ContainsKey(type)
                               ? _providers[type]
                               : GetDefaultGetter(type);

            var poster = _posters.ContainsKey(type)
                             ? _posters[type]
                             : !registration.IsCacheData
                                   ? GetDefaultWriter(type)
                                   : null;

            Debug.Assert(provider != null);
            Debug.Assert(poster != null || registration.IsCacheData);

            var store = new DataStore(registration.ResourceName, provider, poster, _servicesPackage);
                    
            _container.Container.ComposeExportedValue(store);
          

            store.Initialize(registration.IsCacheData);
            
            if (registration.IsSecuritized)
                _securityManager.RegisterSecuritizedType(type);           
              
            _stores.Add(type,store);
        }

        /// <summary>
        /// Производит регистрации всех типов.
        /// </summary>
        /// <param name="type">Тип.</param>
        internal void CommitRegistration(Type type)
        {
            ImplyUpdatePolicy(type);
        }

        /// <summary>
        /// Применяет описанные в ресурсе политики кэширования.
        /// </summary>
        /// <param name="type">Тип ресурса.</param>
        private void ImplyUpdatePolicy(Type type)
        {
            var policies = type.GetCustomAttributes(typeof(CachingPolicyAttribute), false).OfType<CachingPolicyAttribute>();
            DataStore getter;
            try
            {
                getter = _stores[type];
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidOperationException("Не зарегистрирован тип: " + type.Name);
            }

            if (!policies.Any())
                getter.SetNoCachePolicy();

            if (_settingsHolder.Settings.AwareOfInheritance)
            {
                var anc = type.Construct(k => k.BaseType != null, k => k.BaseType)
                              .Skip(1)
                              .FirstOrDefault(k => _registrationStorage.IsResourceRegistered(k));

                if (anc != null)
                    getter.ProcessDependentCaching(anc);
            }

            foreach (var policy in policies)
            {
                if (policy is DependentCachingAttribute)
                {
                    var parentType = (policy as DependentCachingAttribute).ParentType;
                    Debug.Assert(_stores.ContainsKey(parentType), string.Format("Ресурс {0} помечен как зависимый от ресурса {1}, не зарегистрированного в контейнере", type, parentType));
                    if (!_stores.ContainsKey(parentType))
                        throw new InvalidOperationException(string.Format("Ресурс {0} помечен как зависимый от ресурса {1}, не зарегистрированного в контейнере", type, parentType));
                    _stores[parentType].ProcessDependentCaching(type);
                }
                if (policy is DependentQueriedCachingAttribute)
                {
                    var parentType = (policy as DependentQueriedCachingAttribute).ParentType;
                    Debug.Assert(_stores.ContainsKey(parentType));
                    _stores[parentType].ProcessParentQueriedCaching(type);
                    _stores[type].ProcessChildQueriedCaching(_stores[parentType]);
                }
            }
        }


      

        public void Initialize()
        {                    
            try
            {
                foreach (var regisration in _registrationStorage.AllRegistrations)
                {
                    try
                    {
                        PrepareRegistration(regisration);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Ошибка подготовки типа:" + regisration.ResourceName, ex);
                    }
                }

                foreach (var type in _registrationStorage.AllRegistrations.Select(k=>k.ResourceType))
                {
                    try
                    {
                        CommitRegistration(type);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Ошибка регистрации типа:" + type.Name, ex);
                    }
                }                              
            }
            catch (Exception)
            {
                Debugger.Break();
                _registrationStorage.FlushRegistraions();
               //любая ошибка должна чистить регистрации                           
                throw;
            }

        }

        public Dictionary<Type,DataStore>  InitializeAndReturnStores()
        {
            Initialize();
            return _stores;
        }
    }
}
