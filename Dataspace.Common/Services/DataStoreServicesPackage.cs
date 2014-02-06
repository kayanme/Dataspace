using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Statistics;
using Dataspace.Common.Transactions;


namespace Dataspace.Common.Services
{
    [Export]
    internal sealed class DataStoreServicesPackage
    {
        #region Imports

#pragma warning disable 0649
        [Import]
        public IGenericPool GenericPool;

        [Import]
        public IInterchangeCachier _intCachier;
        [Import]
        public QueryStorage _queryStorage;
        [Import]
        public TransactedResourceManager _resourceManager;

        [Import]
        public SettingsHolder _settingsHolder;
        [Import]
        public StatisticsCollector _statisticsCollector;

        [Import]
        public IAnnouncerSubscriptorInt _subscriptor;
#pragma warning restore 0649

        #endregion
    }
}
