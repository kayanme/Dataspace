using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Dataspace.Common.Attributes;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Projections.Storages;
using Dataspace.Common.ServiceResources;
using Dataspace.Common.Services;

namespace Dataspace.Common.ClassesForImplementation
{
    [ContractClass(typeof(ResourceQuerierContracts))]
    public abstract class ResourceQuerier
    {
       
#pragma warning disable 0649

        [Import]
        protected ITypedPool TypedPool { get; private set; }
        
#pragma warning restore 0649

        protected StringComparer ParameterComparer = StringComparer.InvariantCultureIgnoreCase;

        protected EqualityComparer<string> S = EqualityComparer<string>.Default;

        internal abstract IEnumerable<Query> ReturnQueries();             
    }

    public abstract class ResourceQuerier<T> : ResourceQuerier
    {

      
        #region Imports

#pragma warning disable 0649
        [Import(AllowRecomposition = false, RequiredCreationPolicy = CreationPolicy.Shared)]
        private SettingsHolder _settingsHolder;

        [Import]
        private AppConfigProvider _appConfigProvider;

        [Import]
        private RegistrationStorage _registrationStorage;
#pragma warning restore 0649

        #endregion
            
        private IEnumerable<MethodInfo> SelectApplicableMethods()
        {
            if (!_settingsHolder.Settings.ActivationSwitchMatch(GetType().GetCustomAttributes(typeof (ActivationSwitchAttribute), true)
                                                             .OfType<ActivationSwitchAttribute>().ToArray(),_appConfigProvider))
                return new MethodInfo[0];

            var allMethods = GetType().GetMethods()
                                      .Where(k =>Attribute.IsDefined(k,(typeof (IsQueryAttribute))));
            var targetMethods =
               allMethods.Where(
                k => _settingsHolder.Settings
                                    .ActivationSwitchMatch(k.GetCustomAttributes(typeof (ActivationSwitchAttribute), true)
                                                                     .OfType<ActivationSwitchAttribute>(),
                                                           _settingsHolder.Provider));

            return targetMethods;
        }

        internal override sealed IEnumerable<Query> ReturnQueries()
        {            
            var methods = SelectApplicableMethods();
            var spaces = from meth in methods
                         let spc = meth.GetCustomAttributes(typeof (QueryNamespaceAttribute), false)
                             .Cast<QueryNamespaceAttribute>()
                         select spc.Any()
                                    ? spc.Select(k => k.Namespace).ToArray()
                                    : new[] {""};
            var pairs = methods.Zip(spaces, (meth, space) => new {meth, space});
            var key = _registrationStorage[typeof (T)].ResourceKey;
            return pairs.SelectMany(k =>k.space.Select(k2=>Query.CreateFromMethod(key,k2,this, k.meth)));
        }        

        protected readonly IEnumerable<Guid> DefaultValue = new Guid[0];

        protected readonly IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>> SeriesDefaultValue =
            new KeyValuePair<Guid, IEnumerable<Guid>>[0];
    }

    [ContractClassFor(typeof (ResourceQuerier))]
    internal abstract class ResourceQuerierContracts : ResourceQuerier
    {
        internal override IEnumerable<Query> ReturnQueries()
        {
            Contract.Ensures(Contract.Result<IEnumerable<Query>>() != null);
            return null;
        }
        
    }
}
