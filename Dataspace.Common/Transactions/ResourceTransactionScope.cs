using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;

namespace Dataspace.Common.Transactions
{
    public sealed class ResourceTransactionScope:IDisposable
    {

        private readonly TransactionScope _transactionScope;
        private readonly bool _shouldWritePacket;

        public ResourceTransactionScope()
        {
            _shouldWritePacket = (Transaction.Current == null);
            _transactionScope = new TransactionScope();
          
        }

        public ResourceTransactionScope(TransactionScopeOption option)
        {
            _shouldWritePacket = (option == TransactionScopeOption.RequiresNew);
            _transactionScope = new TransactionScope(option);
          
        }

        public ResourceTransactionScope(Transaction transactionToUse)
        {
            _transactionScope = new TransactionScope(transactionToUse);
            _shouldWritePacket = (transactionToUse == null);
        }

        public ResourceTransactionScope(Transaction transactionToUse,TimeSpan timeout)
        {
            _transactionScope = new TransactionScope(transactionToUse,timeout);
            _shouldWritePacket = (transactionToUse == null);
        }

        public ResourceTransactionScope(TransactionScopeOption option, TimeSpan timeout)
        {
            _shouldWritePacket = (option == TransactionScopeOption.RequiresNew || Transaction.Current == null);
            _transactionScope = new TransactionScope(option,timeout);
        
        }

        public ResourceTransactionScope(TransactionScopeOption option,TransactionOptions options)
        {
            _shouldWritePacket = (option == TransactionScopeOption.RequiresNew || Transaction.Current == null);
            _transactionScope = new TransactionScope(option,options);         
        }


        public void Dispose()
        {
            _transactionScope.Dispose();
        }

        public void Complete()
        {
            if (Transaction.Current != null && _shouldWritePacket)
               Transaction.Current.SendResources();
            _transactionScope.Complete();
        }
    }
}
