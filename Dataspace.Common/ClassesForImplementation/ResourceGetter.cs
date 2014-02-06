using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dataspace.Common.Statistics;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Utility;
using Dataspace.Common.Statistics;

namespace Dataspace.Common.ClassesForImplementation
{

    /// <summary>
    /// Провайдер данных.
    /// </summary>
    [Export(typeof(IInitialize))]
    public abstract class ResourceGetter
    {
#pragma warning disable 0649

        [Import]
        protected ITypedPool TypedPool { get; private set; }

        [Import]
        protected IGenericPool GenericPool { get; private set; }

        [Import] 
        internal IStatChannel StatChannel;
               
#pragma warning restore 0649
              
        protected internal bool IsTracking;


        [Pure]
        internal object GetItem(Guid id)
        {
            var t = Stopwatch.StartNew();
            var item = GetItemInt(id);  
            t.Stop();
            StatChannel.SendMessageAboutOneResource(id,Actions.ExternalGet,t.Elapsed);
            return item;
        }

        [Pure]
         protected abstract object GetItemInt(Guid id);


        [Pure]
        internal IEnumerable<KeyValuePair<Guid, object>> GetItems(IEnumerable<Guid> id)
        {
            var t = Stopwatch.StartNew();
            var ids = id.ToArray();
            var items = GetItemsInt(ids);
            t.Stop();
            StatChannel.SendMessageAboutMultipleResources(ids.ToArray(),Actions.ExternalGet,t.Elapsed);
            return items;
        }

        [Pure]
        protected abstract IEnumerable<KeyValuePair<Guid, object>> GetItemsInt(IEnumerable<Guid> id);

        internal abstract IAccumulator<Guid,object> ReturnAccumulator(ICachierStorage<Guid> storage, Func<Guid, object> getResource);
    }


    [ContractClass(typeof(ResourceGetterContract<>))]
    public abstract class ResourceGetter<T> : ResourceGetter where T:class
    {

        [Pure]
        protected sealed override object GetItemInt(Guid id)
        {                           
            return GetItemTyped(id);         
        }

        [Pure]
        protected abstract T GetItemTyped(Guid id);
      
        [Pure]
        protected sealed override IEnumerable<KeyValuePair<Guid, object>> GetItemsInt(IEnumerable<Guid> id)
        {           
            Contract.Ensures(Contract.Result<IEnumerable<object>>() != null);
            var ids = id.ToArray();
            if (ids.Any())
                return GetItemsTyped(ids).Select(k => new KeyValuePair<Guid, object>(k.Key, k.Value));
            return (new KeyValuePair<Guid, object>[0]);
        }

        [Pure]
        protected virtual IEnumerable<KeyValuePair<Guid, T>> GetItemsTyped(IEnumerable<Guid> id)
        {
            return id.ToDictionary(k=>k,GetItemTyped);
        }


        internal sealed override IAccumulator<Guid, object> ReturnAccumulator(ICachierStorage<Guid> storage, Func<Guid, object> getResource)
        {
            return new Accumulator<Guid, T>(storage.Push, storage.HasActualValue,k=> getResource(k) as T, GetItemsTyped);
        } 
              
// ReSharper disable StaticFieldInGenericType
        private static MethodInfo _queryMeth =  typeof (Queryable).GetMethods().Single(k => k.Name == "Contains" && k.GetParameters().Length == 2)
                                                              .MakeGenericMethod(typeof(Guid));
// ReSharper restore StaticFieldInGenericType
                                                                  
        
        [Pure]
        protected static Expression<Func<TTarget, bool>> ConditionBuilder<TTarget>(IEnumerable<Guid> id, string keyName)
        {
            Contract.Requires(_queryMeth != null);
            Contract.Ensures(Contract.Result<Expression<Func<TTarget, bool>>>() != null);
            var expParam = Expression.Parameter(typeof(TTarget));
            id = id is Guid[] ? id : id.ToArray();
            if (!id.Any())
                return Expression.Lambda(Expression.Constant(false), expParam) as Expression<Func<TTarget, bool>>;       
            var condition = Expression.Call(_queryMeth, Expression.Constant(id.AsQueryable()), Expression.PropertyOrField(expParam, keyName));
            
            return Expression.Lambda(condition, expParam) as Expression<Func<TTarget, bool>>;
        }
      
     
    }

    [ContractClassFor(typeof(ResourceGetter<>))]
    internal abstract class ResourceGetterContract<T>:ResourceGetter<T> where T:class
    {
        protected override T GetItemTyped(Guid id)
        {
            return null;
        }

        protected override IEnumerable<KeyValuePair<Guid, T>> GetItemsTyped(IEnumerable<Guid> id)
        {
            Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<Guid, T>>>() != null);
            Contract.Ensures(id.Count() == Contract.Result<IEnumerable<KeyValuePair<Guid, T>>>().Count());
            Contract.Ensures(id.All(k => Contract.Result<IEnumerable<KeyValuePair<Guid, T>>>().Any(k2=>k2.Key == k)));
            return null;
        }
    }
}
