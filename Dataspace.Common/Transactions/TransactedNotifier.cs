using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;
using Indusoft.Common.ResourceLayer.Interfaces;
using Indusoft.Common.ResourceLayer.Interfaces.Internal;
using Indusoft.Common.TransientCachier;


namespace Indusoft.Common.ResourceLayer.Transactions
{
    [Export]
    internal class TransactedNotifier
    {
#pragma warning disable 0649
        [Import]
        private IAnnouncerSubscriptorInt _subscriptor;
#pragma warning restore 0649

        private ConcurrentDictionary<Guid, TransactedUpdatesStorage> _transactionUpdates = new ConcurrentDictionary<Guid, TransactedUpdatesStorage>();              

        public void AddUnactualResource(UnactualResourceContent resource,ICachierStorage<Guid>  resourceStorage)
        {
            if (Transaction.Current == null)
                SendUpdate(resource);
            else
            {
                bool newTransaction = false;
                var transactedResourceStorage =
                    _transactionUpdates.GetOrAdd(Transaction.Current.TransactionInformation.DistributedIdentifier,
                                                 id =>
                                                     {
                                                         newTransaction = true;
                                                         Transaction.Current.TransactionCompleted += Current_TransactionCompleted;
                                                         return new TransactedUpdatesStorage(_subscriptor, Transaction.Current.IsolationLevel);
                                                     });
                if (newTransaction)
                    Transaction.Current.EnlistVolatile(transactedResourceStorage, EnlistmentOptions.None);

                transactedResourceStorage.AddResource(resource, resourceStorage);
            }
        }

        void Current_TransactionCompleted(object sender, TransactionEventArgs e)
        {
            TransactedUpdatesStorage upd;
            _transactionUpdates.TryRemove(e.Transaction.TransactionInformation.DistributedIdentifier, out upd);            
        }

        internal void SendUpdate(UnactualResourceContent resource)
        {
            _subscriptor.AnnonunceUnactuality(resource.ResourceName, resource.ResourceKey);
        }
    }
}
