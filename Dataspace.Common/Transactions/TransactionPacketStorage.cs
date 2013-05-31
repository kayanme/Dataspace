using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
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
    internal class TransactionPacketStorage : IPromotableSinglePhaseNotification, IDisposable
    {

        private readonly SerialPoster _poster;

        private readonly Transaction _holdingTransaction;

        private bool _notOnlyLocalEffects;

        private readonly ReaderWriterLockSlim _resourceWriterQueueLock = new ReaderWriterLockSlim();


        private readonly Queue<DataRecord> _resourcesToWrite = new Queue<DataRecord>();

        private readonly MemoryStream _stream = new MemoryStream();

        private readonly BinaryFormatter _formatter = new BinaryFormatter();

        [ImportingConstructor]
        public TransactionPacketStorage(SerialPoster poster)
        {
          
            Contract.Requires(poster != null);                     
            _holdingTransaction = Transaction.Current;
            _notOnlyLocalEffects = _holdingTransaction.TransactionInformation.DistributedIdentifier != default(Guid) 
                  || _holdingTransaction.IsolationLevel==IsolationLevel.ReadUncommitted
                  || _holdingTransaction.IsolationLevel == IsolationLevel.Chaos;
            _poster = poster;
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
                    try
                    {
                        if (record.Resource != null)
                        {
                            _stream.Position = 0;
                            _formatter.Serialize(_stream, record.Resource);
                            _stream.Position = 0;
                            record.Resource = _formatter.Deserialize(_stream);
                        }
                        _resourceWriterQueueLock.EnterWriteLock();
                        if (existingResource.Content != null) //если найден уже записанный в данной транзакции ресурс
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

        private void WriteResources()
        {
            if (_resourcesToWrite.Any())
            {
                Transaction oldTransaction = null;
                bool transactionRequired = Transaction.Current !=null && _resourcesToWrite.Count > 1;
                DependentTransaction depTrans = null;
                if (transactionRequired)
                {
                    oldTransaction = Transaction.Current;
                    Transaction.Current = depTrans = oldTransaction.DependentClone(DependentCloneOption.RollbackIfNotComplete);                    
                }
                try
                {
                    var resourceQueue = _resourcesToWrite.ToArray();
                    _poster.PostPacket(resourceQueue);
                    _resourcesToWrite.Clear();
                    if (depTrans != null)
                        depTrans.Complete();
                }
                finally
                {
                    if (transactionRequired)
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

        /// <summary>
        /// Notifies an enlisted object that an escalation of the delegated transaction has been requested.
        /// </summary>
        /// <returns>
        /// A transmitter/receiver propagation token that marshals a distributed transaction. For more information, see <see cref="M:System.Transactions.TransactionInterop.GetTransactionFromTransmitterPropagationToken(System.Byte[])" />.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
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
            WriteResources();
            singlePhaseEnlistment.Committed();
        }

        public void Rollback(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            singlePhaseEnlistment.Aborted();
        }
    }
}
