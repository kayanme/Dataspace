using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;

namespace Dataspace.Common.Transactions
{
    public static class TransactionExtensions
    {
        

        public static void SendResources(this Transaction transaction)
        {
            TransactionStoragesStorage.SendResource(transaction);
        }
    }
}
