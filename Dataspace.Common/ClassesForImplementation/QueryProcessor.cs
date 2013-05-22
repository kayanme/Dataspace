using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;

namespace Dataspace.Common.ClassesForImplementation
{
    [ContractClass(typeof(QueryProcessorContracts))]
    public abstract class QueryProcessor
    {
        public abstract IEnumerable<T> GetItems<T>(Expression<Func<T, bool>> predicate) where T : class;
    }

    [ContractClassFor(typeof(QueryProcessor))]
    internal abstract class QueryProcessorContracts : QueryProcessor
    {
        public override IEnumerable<T> GetItems<T>(Expression<Func<T, bool>> predicate) 
        {
            Contract.Requires(predicate!=null);
            Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);
            return null;
        }
    }
}
