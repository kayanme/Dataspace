using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Transactions;
using Dataspace.Common.Announcements;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Announcements;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;


namespace Dataspace.Common.Transactions
{
    internal class TransactedUpdatesStorage :  ISinglePhaseNotification,IDisposable
    {
        private readonly IAnnouncerSubscriptorInt _subscriptor;

        private readonly IsolationLevel _isolation;

        private readonly Action<IEnumerable<DataRecord>> _poster;

        private TransactionScope _scope;

        private Transaction _scopeTransaction;

        private readonly ReaderWriterLockSlim _resourceWriterQueueLock = new ReaderWriterLockSlim();

        private readonly ConcurrentBag<UpdateRecord> _unactualities = new ConcurrentBag<UpdateRecord>();

        private readonly Queue<DataRecord> _resourcesToWrite = new Queue<DataRecord>();

        internal struct DataRecord
        {
            public UnactualResourceContent Content;
            public object Resource;
            public ResourcePoster Poster;
        }

        private class UpdateRecord
        {
            public UnactualResourceContent Resource;
            public ICachierStorage<Guid> Storage;
        }

        public TransactedUpdatesStorage(IAnnouncerSubscriptorInt subscriptor, Action<IEnumerable<DataRecord>> poster, IsolationLevel isolation)
        {
            Contract.Requires(subscriptor != null);
            Contract.Requires(poster != null);         
            _subscriptor = subscriptor;
            _isolation = isolation;
            _poster = poster;
        }
        
        private void SendUpdates()
        {
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                bool isSomething = true;
                while (isSomething)
                {
                    UpdateRecord un;
                    isSomething = _unactualities.TryTake(out un);
                    if (isSomething)
                    {
                        _subscriptor.AnnonunceUnactuality(un.Resource.ResourceName, un.Resource.ResourceKey);
                        if (un.Storage != null)
                            un.Storage.SetUpdateNecessity(un.Resource.ResourceKey);
                    }
                }
                scope.Complete();
            }
        }
       
        private void WriteResources()
        {
            if (_resourcesToWrite.Any())
            {
                Transaction oldTransaction = null;
                bool transactionRequired = _resourcesToWrite.Count > 1;
                if (transactionRequired)
                {
                    oldTransaction = Transaction.Current;
                    _scope = new TransactionScope(TransactionScopeOption.RequiresNew);
                }
                try
                {
                    var resourceQueue = _resourcesToWrite.ToArray();
                    _poster(resourceQueue);
                }
                finally
                {
                    if (transactionRequired)
                    {
                        _scopeTransaction = Transaction.Current;
                        Transaction.Current = oldTransaction;
                    }
                }
            }
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            try
            {
                WriteResources();
            }
            catch (Exception)
            {
                preparingEnlistment.ForceRollback();
                throw;
            }

            preparingEnlistment.Prepared();

        }

        public void Commit(Enlistment enlistment)
        {
           
            SendUpdates();
            if (_scope != null)//его может не быть, если выделенная для записи ресурсов транзакция не создавалась (не было ресурсов для записи или он был один)
            {
                Transaction.Current = _scopeTransaction;
                _scope.Complete();
                _scope.Dispose();
            }
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            try
            {
                if (_scope != null)//его может не быть, если выделенная для записи ресурсов транзакция не создавалась
                {
                    Transaction.Current = _scopeTransaction;
                    _scope.Dispose();
#if DEBUG
                    Debugger.Break(); //к этому моменту она просто обязана завалиться, удивлюсь если нет.
#endif
                }
            }
            catch (TransactionAbortedException)
            {              
            }
            

            switch (_isolation)
            {
                case IsolationLevel.ReadCommitted:                    
                    break;
                default:
                    SendUpdates();
                    break;
            }
            
            enlistment.Done();
        }

        /// <summary>
        /// Notifies an enlisted object that the status of a transaction is in doubt.
        /// </summary>
        /// <param name="enlistment">An <see cref="T:System.Transactions.Enlistment" /> object used to send a response to the transaction manager.</param>
        public void InDoubt(Enlistment enlistment)
        {            
            enlistment.Done();
        }
     
        public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            try
            {
                WriteResources();

                SendUpdates();
                if (_scope != null)//его может не быть, если выделенная для записи ресурсов транзакция не создавалась
                {
                    Transaction.Current = _scopeTransaction;
                    _scope.Complete();
                    _scope.Dispose();
                }
                singlePhaseEnlistment.Committed();
            }
            catch (Exception)
            {
                singlePhaseEnlistment.Aborted();
                throw;
            }                                                           
        }

        public DataRecord GetResource(UnactualResourceContent resource)
        {
            try
            {
                _resourceWriterQueueLock.EnterReadLock();
                return _resourcesToWrite.FirstOrDefault(k=>k.Content.Equals(resource));
            }
            finally
            {
                _resourceWriterQueueLock.ExitReadLock();
            }
        }

        public void AddResourceToPost(DataRecord record)
        {
            try
            {
                _resourceWriterQueueLock.EnterUpgradeableReadLock();
                var existingResource = _resourcesToWrite.FirstOrDefault(k => k.Content.Equals(record.Content));
                try
                {
                    _resourceWriterQueueLock.EnterWriteLock();
                    if (existingResource.Content != null)//если найден уже записанный в данной транзакции ресурс
                        existingResource.Resource = existingResource;
                    else
                       _resourcesToWrite.Enqueue(record);
                }
                finally
                {

                    _resourceWriterQueueLock.ExitWriteLock();
                }

            }
            finally
            {
                _resourceWriterQueueLock.ExitUpgradeableReadLock();
            }
        }

        public void AddResource(UnactualResourceContent resource, ICachierStorage<Guid> storage)
        {
            var record = new UpdateRecord {Resource = resource, Storage = storage};
            switch (_isolation)
            {                
                case IsolationLevel.ReadCommitted :                   
                    break;
                default:
                    storage.SetUpdateNecessity(resource.ResourceKey);
                    break;
            }
            _unactualities.Add(record);
        }

        public void Dispose()
        {
            _resourceWriterQueueLock.Dispose();
            if (_scope !=null)
                _scope.Dispose();
        }
    }
}
