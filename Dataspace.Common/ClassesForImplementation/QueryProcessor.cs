using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
using Dataspace.Common.Transactions;

namespace Dataspace.Common.ClassesForImplementation
{
    [ContractClass(typeof(QueryProcessorContracts))]
    public abstract class QueryProcessor
    {
        [Import] 
        private TransactedResourceManager _transactedResourceManager;

        protected abstract IEnumerable<T> QueryItems<T>(Expression<Func<T, bool>> predicate) where T : class;

        public IEnumerable<T> GetItems<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            if (Transaction.Current !=null)
            {
                return _transactedResourceManager.GetTransactedItems(predicate).Concat(QueryItems(predicate)).ToArray();
            }
            else
            {
                return QueryItems(predicate);
            }
        }
    }

    [ContractClassFor(typeof(QueryProcessor))]
    internal abstract class QueryProcessorContracts : QueryProcessor
    {
        protected override IEnumerable<T> QueryItems<T>(Expression<Func<T, bool>> predicate) 
        {
            Contract.Requires(predicate!=null);
            Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);
            return null;
        }
    }
}
