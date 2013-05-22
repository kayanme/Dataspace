using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Threading;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Interfaces.Internal;
using Common.ParsingServices;

namespace Dataspace.Common.Services
{
    [Export(typeof(ICacheServicing))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ServiceComponent:ICacheServicing
    {

        #region Imports

#pragma warning disable 0649
        [Import]
        private IInterchangeCachier _cachier;

        [Import(AllowDefault = false)]
        private CompositionService _container;

        [Import]
        private SettingsHolder _settings;

        [ImportMany]
        private IEnumerable<IInitialize> _initializers;

#pragma warning restore 0649

        #endregion


        public void CachePanic()
        {
           _cachier.CachePanic();
        }

        private const int Not = 0;
        private const int Initializing = 1;
        private const int Ready = 2;

        private int _state = Not;

        [ThreadStatic]
        private static bool _initializationThread;//указывает, инициализирует ли текущий поток кэш. Нужен для предотвращений дедлока

        public void Initialize()
        {
            var init = Interlocked.CompareExchange(ref _state, Initializing, Not);
            if (init == Not)
            {
                try
                {
                    _initializationThread = true;
                   if (_container != null)
                       ParsingBlock.SetMefresolver(_container.Container);
                    foreach (var initializer in _initializers.OrderBy(k => k.Order))
                    {
                        initializer.Initialize();
                    }
                    IsInitialized = true;                    
                }
                catch 
                {
                    _state = Not;
                    throw;
                }
                finally
                {
                    _initializationThread = false;
                }
                _state = Ready;
            }
            else if (init == Initializing && !_initializationThread)
            {
                SpinWait.SpinUntil(() => _state == Ready);
            }
        }

        public void Initialize(Settings settings,CompositionContainer container = null)
        {
            if (container!=null)
               _container.SetContainer(container);
            _settings.Settings = settings;
            Initialize();
        }


        public bool IsInitialized { get; private set; }
    }
}
