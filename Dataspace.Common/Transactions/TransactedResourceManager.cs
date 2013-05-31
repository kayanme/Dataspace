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
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;


namespace Dataspace.Common.Transactions
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class TransactedResourceManager
    {
#pragma warning disable 0649
   
        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)] 
        private Lazy<TransactionPacketStorage>   _transactedResourceStore;

        [Import(RequiredCreationPolicy = CreationPolicy.Shared)] 
        private TransactionStoragesStorage _centralStore;
#pragma warning restore 0649



        public void AddResourceToSend(UnactualResourceContent description, object resource,
                                      Action<Guid,object> poster)
        {
            Contract.Requires(Transaction.Current != null);
            if (Transaction.Current == null)
                poster(description.ResourceKey, resource);
            else
            {
                var transactedResourceStorage = _centralStore.GetResourceStorage(_transactedResourceStore);
                transactedResourceStorage.AddResourceToPost(new DataRecord
                {
                    Content = description,
                    Resource = resource,
                    Poster = poster
                });    
            }
            
        }

        public DataRecord GetResource(UnactualResourceContent description)
        {
            Contract.Requires(Transaction.Current != null);
            var transactedResourceStorage = _centralStore.GetResourceStorage(_transactedResourceStore);
            return transactedResourceStorage.GetResource(description);
        }

         


    }
}

    