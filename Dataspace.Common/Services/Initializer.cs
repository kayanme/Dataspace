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

namespace Dataspace.Common.Services
{
    [Export]
    internal sealed class Initializer
    {

        #region Imports

#pragma warning disable 0649
        //собственно, они (точнее создаватели по умолчанию)      
        [Import(AllowDefault = true)]
        private IResourceGetterFactory _getterFactory;

        [Import(AllowDefault = true)]
        private IResourcePosterFactory _posterFactory;

        [Import(AllowDefault = true)]
        private CompositionService _container;
              
        [Import(AllowRecomposition = false)]
        private SecurityManager _securityManager;

        //это все регистраторы ресурсов. Каждый ресурс, с которым требуется работать, должен быть зарегистрирован.
        //где располагать регистраторы - дело хозяйское
        [ImportMany(typeof(ResourceRegistrator))]
        private IEnumerable<ResourceRegistrator> _registrators;
     
        [Import(AllowRecomposition = false)]
        private DefaultFabrica _fabrica;

        [Import(AllowRecomposition = false,RequiredCreationPolicy = CreationPolicy.Shared)]
        private QueryStorage _storage;

        [Import(AllowRecomposition = false, RequiredCreationPolicy = CreationPolicy.Shared)]
        private SettingsHolder _settingsHolder;

        [Import(AllowRecomposition = false, RequiredCreationPolicy = CreationPolicy.Shared)]
        private RegistrationStorage _registrationStorage;

        [ImportMany(typeof (ResourceGetter))] 
        private IEnumerable<Lazy<ResourceGetter,ActivationSwitchAttribute>> getters;

        [ImportMany(typeof (ResourcePoster))] 
        private IEnumerable<Lazy<ResourcePoster,ActivationSwitchAttribute>> writers;
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

        private static void TestTypeSerialization(Type type)
        {

            var isSerializable = Attribute.IsDefined(type, typeof(SerializableAttribute));

            Debug.Assert(isSerializable, "Ресурс (" + type.Name + ") должен быть сериализуемым и помеченным атрибутом [Serializable]");
            if (!isSerializable)
                throw new InvalidOperationException("Ресурс (" + type.Name + ") должен быть сериализуемым и помеченным атрибутом [Serializable]");

            using (var s = new MemoryStream())
            {
                try
                {
                    new BinaryFormatter().Serialize(s, Activator.CreateInstance(type));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Тестовая сериализация ресурса " + type.FullName + " неудачна.", ex);
                }

            }
        }

       
        internal void PrepareRegistration(Type type)
        {

         

            Debug.Assert(
              getters.All(
                  k => k.GetType().BaseType != null && k.GetType().BaseType.GetGenericArguments().Length == 1));

            Func<ResourceGetter, Type> provType =
                k => k.GetType().BaseType.GetGenericArguments().First();

            Func<ResourcePoster, Type> wrType =
               k => k.GetType().BaseType.GetGenericArguments().First();
            //все известны кастомные получатели-писатели заполняем сразу. Потом добавим по умолчанию кому надо.
        
            _providers = getters.Where(k =>_settingsHolder.Settings.ActivationSwitchMatch(k.Metadata,_settingsHolder.Provider))
                                .GroupBy(k => provType(k.Value))
                                .Select(k=>k.First().Value)
                                .ToDictionary(provType);

            _posters = writers.Where(k => _settingsHolder.Settings.ActivationSwitchMatch(k.Metadata, _settingsHolder.Provider))
                                .GroupBy(k => wrType(k.Value))
                                .Select(k => k.First().Value)
                                .ToDictionary(wrType);

            //определение ресурса
            var definition = Attribute.GetCustomAttribute(type, typeof(ResourceAttribute)) as ResourceAttribute;
            //или кэш-данных
            var cacheData = Attribute.GetCustomAttribute(type, typeof(CachingDataAttribute)) as CachingDataAttribute;
            //защищен ли ресурс настройками безопасности
            var securitized = Attribute.GetCustomAttribute(type, typeof(SecuritizedAttribute)) as SecuritizedAttribute;
            string name;

            if (definition != null)//если это ресурс
            {
                Debug.Assert(cacheData == null, "Либо ресурс, либо данные для кэширования!");
                if (cacheData != null)
                    throw new InvalidOperationException("Либо ресурс, либо данные для кэширования!");

                TestTypeSerialization(type);
                name = definition.Name;

            }
            else if (cacheData != null)//если это кэш-данные
            {
                name = cacheData.Name;
            }
            else
            {
                Debug.Fail("Регистрируемый объект должен быть помечен либо атрибутом [Resource], либо атрибутом [CacheData]. ");
                throw new InvalidOperationException("Регистрируемый объект должен быть помечен либо атрибутом [Resource], либо атрибутом [CacheData]. ");
            }

            _registrationStorage.AddRegistrations(type, name);
            var provider = _providers.ContainsKey(type)
                               ? _providers[type]
                               : GetDefaultGetter(type);

            var poster = _posters.ContainsKey(type)
                             ? _posters[type]
                             : cacheData == null
                                   ? GetDefaultWriter(type)
                                   : null;

            Debug.Assert(provider != null);
            Debug.Assert(poster != null || cacheData != null);

            var store = new DataStore(name,provider, poster);
          
            _container.Container.SatisfyImportsOnce(store);
            _container.Container.ComposeExportedValue(store);
          

            store.Initialize(cacheData != null);
            
            if (securitized != null)
                _securityManager.RegisterSecuritizedType(type);           

            if ((cacheData == null || cacheData.DefaultQuerier))  //для кэш-данных необязательно нужен запросчик
                _storage.RegisterType(type);

          
            Debug.Assert(_storage.TypeRegistered(type) || cacheData != null);

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


            _registrationStorage.FlushRegistraions();//их быть не должно
                     
            try
            {
                foreach (var type in _registrators.SelectMany(k => k.ResourceTypesInt))
                {
                    try
                    {
                        PrepareRegistration(type);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Ошибка подготовки типа:" + type.Name, ex);
                    }
                }

                foreach (var type in _registrators.SelectMany(k => k.ResourceTypesInt))
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
