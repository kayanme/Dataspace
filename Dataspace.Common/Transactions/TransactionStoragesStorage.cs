using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Transactions;

namespace Dataspace.Common.Transactions
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TransactionStoragesStorage
    {
       
        private readonly Dictionary<Transaction, TransactionPacketStorage> _transactionResources = new Dictionary<Transaction, TransactionPacketStorage>();

       
        private T GetStorage<T>(Func<T> creator, Dictionary<Transaction, T> localstorage) where T : IEnlistmentNotification
        {
            lock (localstorage)
            {
                var key = Transaction.Current;
                if (localstorage.ContainsKey(key))
                    return localstorage[key];
                var storage = creator();
                localstorage.Add(key,storage);
                Transaction.Current.TransactionCompleted += KillStorage<T>;
                Transaction.Current.EnlistVolatile(storage,EnlistmentOptions.EnlistDuringPrepareRequired);
               
                return storage;
            }
        }

        public TransactionPacketStorage GetResourceStorage(Func<TransactionPacketStorage> creator)
        {
            return GetStorage(creator, _transactionResources);
        }

        private void KillStorage<T>(object sender, TransactionEventArgs e)
        {
          
            if (typeof (T) == typeof (TransactionPacketStorage))
                lock (_transactionResources)
                {
                    var key = e.Transaction;
                    if (!_transactionResources.ContainsKey(key))
                        Debugger.Break();
                    var t = _transactionResources[key];
                    t.Dispose();
                    _transactionResources.Remove(key);
                }

        }
    }
}
