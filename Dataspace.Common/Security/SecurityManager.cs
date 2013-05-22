using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation.Security;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Services;
using Dataspace.Common.Interfaces.Internal;

namespace Dataspace.Common.Security
{
    [Export(typeof(SecurityManager))]
    [Export(typeof(ISecurityManager))]
    [Export(typeof(IInitialize))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SecurityManager : ISecurityManager,IInitialize
    {
#pragma warning disable 0649

        [Import(AllowDefault = true, RequiredCreationPolicy = CreationPolicy.Shared)]
        private SecurityContextFactory _securityContextProvider;

        [Import(AllowDefault = true, RequiredCreationPolicy = CreationPolicy.Shared)]
        private IDefaultSecurityProvider _defaultProvider;

        [ImportMany(AllowRecomposition = false)]
        private IEnumerable<Lazy<SecurityProvider,ActivationSwitchAttribute>> _getters;
      
        [Import]
        private IGenericPool _cachier;

        [Import]
        private SettingsHolder _settingsHolder;

        [Import]
        private DefaultFabrica _fabrica;

        [Import]
        private ICacheServicing _servicing;

#pragma warning restore 0649

        private Dictionary<Type, SecurityProvider> _preparsedGetters;

        private Dictionary<Type, SecurityProvider> _providers = new Dictionary<Type, SecurityProvider>();

       

        private SecurityToken CreateDefaultToken()
        {
            return new SecurityToken(true,true);
        }

        private void CheckInitialization()
        {
            if (!_servicing.IsInitialized)
                _servicing.Initialize();
        }

        public SecurityToken GetToken(Type type,Guid id)
        {
            CheckInitialization();
            if (!_providers.ContainsKey(type))
                return CreateDefaultToken();
            else
            {
                var provider = _providers[type];
                return provider.GetToken(id);    
            }            
        }

        public Lazy<SecurityToken> GetTokenDeferred(Type type, Guid id)
        {
            Contract.Requires(type != null);
            Contract.Ensures(Contract.Result<Lazy<SecurityToken>>() != null);
            CheckInitialization();
            if (!_providers.ContainsKey(type))
                return new Lazy<SecurityToken>(CreateDefaultToken);
            else
            {
                var provider = _providers[type];
                return provider.GetTokenDeferred(id);
            }
        }

        internal void RegisterSecuritizedType(Type type)
        {
            CheckInitialization();
            Debug.Assert(_securityContextProvider !=null);
            SecurityProvider provider;
            if (_preparsedGetters.ContainsKey(type))
                provider = _preparsedGetters[type];
            else
            {
                provider = _fabrica.DefaultCreator<SecurityProvider>("поставщик безопасности", "CreateProvider", type, _defaultProvider);
            }

            provider.SetDataCache(_cachier.GetNameByType(type));
            _providers.Add(type,provider);
        }

     

        public void UpdateSecurity(Type type, Guid id)
        {
            CheckInitialization();
            if (_providers.ContainsKey(type))
                _providers[type].SetSecurityUnactual(id);
        }

        public void UpdateSecurity(Type type)
        {
            CheckInitialization();
            if (_providers.ContainsKey(type))
                _providers[type].SetSecurityUnactual();
        }

        public void UpdateSecurity()
        {
            CheckInitialization();
            foreach(var prov in _providers.Values)
                prov.SetSecurityUnactual();
        }

        public SecurityToken GetToken<T>(Guid id)
        {
            CheckInitialization();
            return GetToken(typeof (T), id);
        }

        public SecurityToken GetToken(string name,Guid id)
        {
            CheckInitialization();
            if (!_cachier.IsRegistered(name))
                return CreateDefaultToken();
            return GetToken(_cachier.GetTypeByName(name), id);
        }

        public Lazy<SecurityToken> GetTokenLater<T>(Guid id)
        {
            CheckInitialization();
            return GetTokenDeferred(typeof(T), id);
        }

        public Lazy<SecurityToken> GetTokenLater(string name, Guid id)
        {         
            CheckInitialization();          
            if (!_cachier.IsRegistered(name))
                return new Lazy<SecurityToken>(CreateDefaultToken);
            return GetTokenDeferred(_cachier.GetTypeByName(name), id);
        }

        public int Order { get { return 5; } }
        public void Initialize()
        {
            _preparsedGetters = _getters
                       .Where(k =>_settingsHolder.Settings.FlagsMatch(k.Metadata.Switch as Enum[]))
                       .GroupBy(k => k.Value.GetType().BaseType.GetGenericArguments()[0])
                       .Select(k=>k.First().Value)
                       .ToDictionary(k => k.GetType().BaseType.GetGenericArguments()[0]);
        }
    }
}
