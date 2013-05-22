using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Transactions;
using Dataspace.Common;
using Dataspace.Common.Announcements;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Resources.Test.Providers;
using Resources.Test.TestResources;
using Testhelper;

namespace Resources.Notification.Test.Resource.Level2Providers
{
    internal class Level2Activator:MarshalByRefObject
    {
        protected Assembly[] AssembliesWhichShouldProvideExport
        {
            get { return new[] {  typeof(ITypedPool).Assembly }; }
        }

        protected Type[] ExportedTypes
        {
            get
            {
                return new[]
                           {
                               typeof (Registrator),
                               typeof (NotifiedElementGetter),
                               typeof (UnnotifiedElementGetter),
                               typeof (NotifiedElementPoster),
                               typeof (UnnotifiedElementPoster),                          
                               typeof(Factory),
                               typeof(Querier),
                               typeof(Writer),
                               typeof(DownlinkLevel2)
                           };
            }
        }

        private CompositionContainer container;

        internal CompositionContainer Container { get { return container; } }

        public void SetTransporter(TransporterToLevel2 transporter,Settings settings)
        {
            container = MockHelper.InitializeContainer(AssembliesWhichShouldProvideExport, ExportedTypes);            
            var pool = new ResourcePool();
            container.ComposeExportedValue(pool);
            container.ComposeExportedValue(container);
            var link = new LinkToTransporter{Transporter = transporter};
            container.ComposeExportedValue(link);            
            container.GetExportedValue<ICacheServicing>().Initialize(settings);
        }
        

        private SubscriptionToken _token;

        private List<ResourceDescription> _changed = new List<ResourceDescription>();

        public IEnumerable<ResourceDescription> CheckSubscriptionCameFromLastCheck()
        {
            var t = _changed.ToArray();
            _changed.Clear();
            return t;
        }

        public void Subscribe()
        {
            var subscriptor = container.GetExportedValue<IAnnouncerSubscriptor>();
            _token = subscriptor.SubscribeForResourceChange<NotifiedElement>();
            _token.ResourceMarkedAsUnactual += _token_ResourceMarkedAsUnactual;
        }

        void _token_ResourceMarkedAsUnactual(object sender, SubscriptionToken.ResourceUnactualEventArgs eventArgs)
        {
            _changed.Add(eventArgs.Resource);
        }

        public void Unsubscribe()
        {
            var subscriptor = container.GetExportedValue<IAnnouncerSubscriptor>();
            _token.ResourceMarkedAsUnactual -= _token_ResourceMarkedAsUnactual;
            subscriptor.UnsubscribeForResourceChange(_token);
        }

        private Transaction _transaction;

        public void Post<T>(Guid key,T resource,Transaction transaction = null)
        {
            try
            {
                if (transaction == null && _transaction == null)
                    container.GetExportedValue<ITypedPool>().Post(key, resource);
                else
                {
                    Transaction.Current = _transaction = (transaction ?? _transaction);
                    container.GetExportedValue<ITypedPool>().Post(key, resource);
                }
            }
            catch (Exception ex) 
            {
                
                throw;
            }
            
        }

        public T Get<T>(Guid key) where T:class
        {
            var cachier = container.GetExportedValue<ITypedPool>();
            return cachier.Get<T>(key);
        }

        public void Complete()
        {
            new TransactionScope(_transaction).Complete();
        }
    }
}
