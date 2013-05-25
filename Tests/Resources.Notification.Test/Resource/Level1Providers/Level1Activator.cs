using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Transactions;
using Dataspace.Common.Interfaces;
using Resources.Test.Providers;
using Resources.Test.TestResources;
using Testhelper;

namespace Resources.Notification.Test.Resource.Level1Providers
{
    public class Level1Activator:MarshalByRefObject
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
                               typeof(Writer),
                               typeof(UplinkLevel1),
                               typeof(TransporterToLevel2)
                           };
            }
        }

        private CompositionContainer container;


       
        public Level1Activator()
        {
            container = MockHelper.InitializeContainer(AssembliesWhichShouldProvideExport, ExportedTypes);
            var pool = new ResourcePool();
            container.ComposeExportedValue(pool);
            container.ComposeExportedValue(container);
            Transporter = container.GetExportedValue<TransporterToLevel2>();
            Transporter._activator = this;
            container.GetExportedValue<ICacheServicing>().Initialize();
        }

        public TransporterToLevel2 Transporter
        { get; private set; }

        private Transaction _transaction;

        public void Post<T>(Guid key, T resource, Transaction transaction = null)
        {
            if (transaction == null && _transaction == null)
               container.GetExportedValue<ITypedPool>().Post(key, resource);
            else
            {
                Transaction.Current = _transaction = (transaction ?? _transaction);
                container.GetExportedValue<ITypedPool>().Post(key, resource);
            }
        }


       
        public void Complete()
        {
            new TransactionScope(_transaction).Complete();
        }

        public T Get<T>(Guid key) where T:class
        {
            return container.GetExportedValue<ITypedPool>().Get<T>(key);
        }
    }
}
