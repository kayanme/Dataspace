using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Transactions;
using Dataspace.Common;
using Dataspace.Common.Announcements;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Services;


namespace Dataspace.Common.Transactions
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class TransactedResourceManager
    {
#pragma warning disable 0649
        [Import]
        private IGenericPool _pool;

        [Import] 
        private SerialPoster  _poster;

        [Import(RequiredCreationPolicy = CreationPolicy.Shared)] 
        private TransactionStoragesStorage _centralStore;

        [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
        private DependentTransactionRepository _dependentTransactionRepository;
#pragma warning restore 0649



        public void AddResourceToSend(UnactualResourceContent description, object resource,
                                      Action<Guid, object> poster, Action updateSender)
        {
            Contract.Requires(Transaction.Current != null);
            if (Transaction.Current == null || _dependentTransactionRepository.IsOurTransaction(Transaction.Current))
            {
                poster(description.ResourceKey, resource);
            }
            else
            {
                var transactedResourceStorage =
                    _centralStore.GetResourceStorage(
                        () => new TransactionPacketStorage(_poster, _dependentTransactionRepository));
                transactedResourceStorage.AddResourceToPost(new DataRecord(updateSender, poster, resource, description));
            }
        }


        public void AddResourceToCurrentTransaction(UnactualResourceContent description, object resource)
        {
            Contract.Requires(Transaction.Current != null);
            if (Transaction.Current != null && !_dependentTransactionRepository.IsOurTransaction(Transaction.Current))
            {
                var transactedResourceStorage = _centralStore.GetResourceStorage(() => new TransactionPacketStorage(_poster, _dependentTransactionRepository));
                transactedResourceStorage.AddResourceToLocalTransaction(description, resource);
            }

        }

        public IEnumerable<T> GetTransactedItems<T>(Expression<Func<T,bool>> query)
        {
            Contract.Requires(Transaction.Current != null);
            if (!_dependentTransactionRepository.IsOurTransaction(Transaction.Current))
            {
                var transactedResourceStorage = _centralStore.GetResourceStorage(() => new TransactionPacketStorage(_poster, _dependentTransactionRepository));
                var name = _pool.GetNameByType(typeof (T));
                var func = query.Compile();
                return transactedResourceStorage.QueryResources(name,k=>func((T)k)).Select(k=>k.Resource).OfType<T>();
            }
            else
            {
                return new T[0];
            }          
        }

        public DataRecord GetResource(UnactualResourceContent description)
        {
            Contract.Requires(Transaction.Current != null);
            var transactedResourceStorage = _centralStore.GetResourceStorage(()=>new TransactionPacketStorage(_poster,_dependentTransactionRepository));           
            return transactedResourceStorage.GetResource(description);
        }

         


    }
}

    