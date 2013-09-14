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
    internal class DependentTransactionRepository
    {
        private List<Transaction> _transactions = new List<Transaction>();

        public void PlaceOurTransaction(Transaction transaction)
        {
            lock (_transactions)
            {
                _transactions.Add(transaction);
                transaction.TransactionCompleted +=
                    (o, e) =>
                    {
                        lock (_transactions)
                            _transactions.Remove(transaction);
                    };

            }
        }

        public bool IsOurTransaction(Transaction transaction)
        {
            Debug.Assert(transaction != null);
            lock (_transactions)
                return _transactions.Contains(transaction);
        }
    }
}
