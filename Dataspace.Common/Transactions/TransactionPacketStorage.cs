using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Transactions;
using Dataspace.Common.Announcements;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Services;

namespace Dataspace.Common.Transactions
{
    [Export]
    internal class TransactionPacketStorage : IPromotableSinglePhaseNotification,IEnlistmentNotification, IDisposable
    {
        private readonly DependentTransactionRepository _dependentTransactionRepository;

        private readonly SerialPoster _poster;

        private readonly Transaction _holdingTransaction;

        private bool _notOnlyLocalEffects;

        private readonly ReaderWriterLockSlim _resourceWriterQueueLock = new ReaderWriterLockSlim();


        private readonly Queue<DataRecord> _resourcesToWrite = new Queue<DataRecord>();

        private readonly List<DataRecord> _resourcesInLocalTransaction = new List<DataRecord>();

        private readonly MemoryStream _stream = new MemoryStream();

        private readonly BinaryFormatter _formatter = new BinaryFormatter();

        [ImportingConstructor]
        public TransactionPacketStorage(SerialPoster poster,DependentTransactionRepository dependentTransactionRepository)
        {
          
            Contract.Requires(poster != null);                     
            _holdingTransaction = Transaction.Current;
            _notOnlyLocalEffects = _holdingTransaction.TransactionInformation.DistributedIdentifier != default(Guid) 
                  || _holdingTransaction.IsolationLevel==IsolationLevel.ReadUncommitted
                  || _holdingTransaction.IsolationLevel == IsolationLevel.Chaos;
            _poster = poster;
            _dependentTransactionRepository = dependentTransactionRepository;
            _holdingTransaction.TransactionCompleted += HoldingTransactionTransactionCompleted;
        }

        void HoldingTransactionTransactionCompleted(object sender, TransactionEventArgs e)
        {
           if (e.Transaction.TransactionInformation.Status == TransactionStatus.Committed)
               WriteResources();
           _holdingTransaction.TransactionCompleted -= HoldingTransactionTransactionCompleted;
        }

        public DataRecord GetResource(UnactualResourceContent resource)
        {
            try
            {
                _resourceWriterQueueLock.EnterReadLock();
                return _resourcesToWrite.FirstOrDefault(k => k.Content.Equals(resource));
            }
            finally
            {
                _resourceWriterQueueLock.ExitReadLock();
            }
        }

        public IEnumerable<DataRecord> QueryResources(string type,Func<object,bool> query)
        {
            return _resourcesInLocalTransaction.Concat(_resourcesToWrite)
                                    .Where(k => k.Content.ResourceName == type && k.Resource != null)                
                                    .Where(k=>query(k.Resource));
        }

        public void AddResourceToPost(DataRecord record)
        {
            if (_notOnlyLocalEffects)               
                    record.Poster(record.Content.ResourceKey, record.Resource);                
            else
            {
                try
                {
                    _resourceWriterQueueLock.EnterUpgradeableReadLock();
                    var existingResource = _resourcesToWrite.FirstOrDefault(k => k.Content.Equals(record.Content));
                    if (record.Resource != null)
                    {
                        if (record.Resource is ICloneable)
			{
                             record.Resource =  (record.Resource as ICloneable).Clone();
			}	
                        else if (record.Resource.GetType().IsSerializable)
			{
                          _stream.Position = 0;
                          _formatter.Serialize(_stream, record.Resource);
                           _stream.Position = 0;
                           record.Resource = _formatter.Deserialize(_stream);
			}
			
                    }
                    try
                    {                       
                        _resourceWriterQueueLock.EnterWriteLock();
                        if (existingResource != null) //если найден уже записанный в данной транзакции ресурс
                            existingResource.Resource = record.Resource;
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
        }

        public void AddResourceToLocalTransaction(UnactualResourceContent description,object resource)
        {
            
                try
                {
                    _resourceWriterQueueLock.EnterUpgradeableReadLock();
                    var existingResource = _resourcesInLocalTransaction.FirstOrDefault(k => k.Content.Equals(description));                  
                    try
                    {
                        _resourceWriterQueueLock.EnterWriteLock();
                        if (existingResource != null) //если найден уже записанный в данной транзакции ресурс
                            existingResource.Resource = resource;
                        else
                            _resourcesInLocalTransaction.Add(new DataRecord(description,resource));
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

        internal void WriteResources()
        {
            if (_resourcesToWrite.Any())
            {
                Debug.Assert(_holdingTransaction != null, "_holdingTransaction!=null");
                Transaction oldTransaction = null;
                bool newTransactionRequired = Transaction.Current != _holdingTransaction;
                CommittableTransaction depTrans = null;
                if (newTransactionRequired)
                {
                  
                    oldTransaction = Transaction.Current;
                    Transaction.Current = depTrans = new CommittableTransaction();
                    _dependentTransactionRepository.PlaceOurTransaction(depTrans);
                }
                else
                {
                    Debug.Assert(_holdingTransaction.TransactionInformation.Status == TransactionStatus.Active);
                    _dependentTransactionRepository.PlaceOurTransaction(_holdingTransaction);
                }
                try
                {
                    var resourceQueue = _resourcesToWrite.ToArray();
                    _poster.PostPacket(resourceQueue);
                    _resourcesToWrite.Clear();
                    if (depTrans != null)
                        depTrans.Commit();
                }
                finally
                {
                    if (newTransactionRequired)
                    {
                        Transaction.Current = oldTransaction;
                    }
                }
            }
        }

        public void Dispose()
        {
            _resourceWriterQueueLock.Dispose();        
            _stream.Dispose();
            
        }
    
        public byte[] Promote()
        {
            _notOnlyLocalEffects = true;
            WriteResources();
            return TransactionInterop.GetTransmitterPropagationToken(Transaction.Current);
        }

        public void Initialize()
        {           
        }

        public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            try
            {
                WriteResources();
                singlePhaseEnlistment.Committed();
            }
            catch
            {
                singlePhaseEnlistment.Aborted();
                throw;
            }
        }

        public void Rollback(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            singlePhaseEnlistment.Aborted();
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
           enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
    }
}
