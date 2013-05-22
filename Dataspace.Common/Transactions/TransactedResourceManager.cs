using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Transactions;
using Dataspace.Common;
using Dataspace.Common.Announcements;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;


namespace Dataspace.Common.Transactions
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IInitialize))]
    internal class TransactedResourceManager:IInitialize 
    {
#pragma warning disable 0649
        [Import]
        private IAnnouncerSubscriptorInt _subscriptor;

        [Import(AllowDefault = true)]
        private IResourcePosterFactory _writerFactory;
#pragma warning restore 0649

        private Action<IEnumerable<TransactedUpdatesStorage.DataRecord>> _seriaPoster;

        private readonly ConcurrentDictionary<Guid, TransactedUpdatesStorage> _transactionUpdates = new ConcurrentDictionary<Guid, TransactedUpdatesStorage>();              

        private TransactedUpdatesStorage GetStorage()
        {
            bool newTransaction = false;
            var transactedResourceStorage =
                _transactionUpdates.GetOrAdd(Transaction.Current.TransactionInformation.DistributedIdentifier,
                                             id =>
                                             {
                                                 newTransaction = true;
                                                 Transaction.Current.TransactionCompleted += CurrentTransactionCompleted;
                                                 return new TransactedUpdatesStorage(_subscriptor,_seriaPoster, Transaction.Current.IsolationLevel);
                                             });
            if (newTransaction)
                Transaction.Current.EnlistVolatile(transactedResourceStorage, EnlistmentOptions.None);
            return transactedResourceStorage;
        }

        public void AddUnactualResource(UnactualResourceContent resource, ICachierStorage<Guid> resourceStorage)
        {
            Contract.Requires(Transaction.Current != null);
            var transactedResourceStorage = GetStorage();
            transactedResourceStorage.AddResource(resource, resourceStorage);
        }

        public void AddResourceToSend(UnactualResourceContent description, object resource, ResourcePoster resourceStorage)
        {
            Contract.Requires(Transaction.Current != null);
            var transactedResourceStorage = GetStorage();
            transactedResourceStorage.AddResourceToPost(new TransactedUpdatesStorage.DataRecord { Content = description, Resource = resource, Poster = resourceStorage });
        }

        public TransactedUpdatesStorage.DataRecord GetResource(UnactualResourceContent description)
        {
            Contract.Requires(Transaction.Current != null);
            var transactedResourceStorage = GetStorage();
            return transactedResourceStorage.GetResource(description);
        }

        void CurrentTransactionCompleted(object sender, TransactionEventArgs e)
        {
            TransactedUpdatesStorage upd;
            _transactionUpdates.TryRemove(e.Transaction.TransactionInformation.DistributedIdentifier, out upd);            
        }

        internal void SendUpdate(UnactualResourceContent resource)
        {
            _subscriptor.AnnonunceUnactuality(resource.ResourceName, resource.ResourceKey);
        }

        public int Order { get { return 4; } }
        public void Initialize()
        {
           
            var writer = _writerFactory.ByDefault(k=>k.ReturnSerialWriter());
            if (writer != null)
                _seriaPoster = k => writer(k.Select(k2 => Tuple.Create(k2.Content, k2.Resource)));
            else
            {
                _seriaPoster = k =>
                                   {
                                       foreach (var resource in k)
                                       {
                                           if (resource.Resource == null)
                                               resource.Poster.DeleteResourceRegardlessofTransaction(
                                                   resource.Content.ResourceKey);
                                           else
                                               resource.Poster.WriteResourceRegardlessofTransaction(
                                                   resource.Content.ResourceKey, resource.Resource);
                                       }
                                   };
            }
        }
    }
}
