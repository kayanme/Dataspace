using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Dataspace.Common.Data;

namespace Dataspace.Common.Interfaces
{
    /// <summary>
    /// Провайдер для доступа к ресурсам по имени. 
    /// </summary>
   [ContractClass(typeof(GenericPoolContracts))]
    public interface IGenericPool
    {
        /// <summary>
        /// Posts the name of the resource by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="id">The id.</param>
        /// <param name="resource">The resource.</param>
        void Post(string name, Guid id, object resource);

        /// <summary>
        /// Получает имя ресурса по типу.
        /// </summary>
        /// <param name="type">Тип.</param>
        /// <returns>Имя ресурса</returns>
        [Pure]
        string GetNameByType(Type type);

        /// <summary>
        /// Получает тип ресурса по имени.
        /// </summary>
        /// <param name="name">Имя.</param>
        /// <returns>Тип ресурса</returns>
        [Pure]
        Type GetTypeByName(string name);

        /// <summary>
        /// Получает ресурс по запросу.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="query">The query.</param>
        /// <param name="nmspace">Пространство имен запроса </param>
        /// <returns></returns>
        [Pure]
        IEnumerable<Guid> Query(string name, UriQuery query,string nmspace = "");

        /// <summary>
        /// Отложенно получает ресурс по имени.
        /// </summary>
        /// <param name="name">Имя.</param>
        /// <param name="id">Ключ.</param>     
        /// <returns>Обертка для отложенного получения ресурса.</returns>
        [Pure]
        Lazy<object> GetLater(string name, Guid id);

        /// <summary>
        /// Получение ресурса по имени и ключу.
        /// </summary>
        /// <param name="name">Имя.</param>
        /// <param name="id">Ключ.</param>        
        /// <returns>Ресурс</returns>
        [Pure]
        object Get(string name, Guid id);

        /// <summary>
        /// Получение ресурса по имени и ключу.
        /// </summary>
        /// <returns>Ресурс</returns>
        [Pure]
        IEnumerable<Type> GetResourceTypes();

        /// <summary>
        /// Определение, определен ли ресурс с данным именем.
        /// </summary>
        /// <param name="name">Имя.</param>
        /// <returns>Ресурс ли.</returns>
        [Pure]
        bool IsRegistered(string name);


        /// <summary>
        /// Помечает ресурс как неактуальный
        /// </summary>
        /// <param name="name">Имя ресурса.</param>
        /// <param name="id">Ключ ресурса</param>
        void SetAsUnactual(string name, Guid id);
    }


    [ContractClassFor(typeof(IGenericPool))]
    internal abstract class GenericPoolContracts : IGenericPool
    {
        public void Post(string name, Guid id, object resource)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));
        }

        public string GetNameByType(Type type)
        {
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));
            return null;
        }

        public Type GetTypeByName(string name)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));
            return null;
        }

        public IEnumerable<Guid> Query(string name, UriQuery query, string nmspace = "")
        {
            Contract.Requires(!string.IsNullOrEmpty(nmspace));
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Requires(query!=null);
            Contract.Ensures(Contract.Result<IEnumerable<Guid>>() != null);
            return null;
        }

        public Lazy<object> GetLater(string name, Guid id)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Ensures(Contract.Result<Lazy<object>>() != null);
            return null;
        }

        public object Get(string name, Guid id)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));
            return null;
        }

        public IEnumerable<Type> GetResourceTypes()
        {
            Contract.Ensures(Contract.Result<IEnumerable<Type>>() != null);
            return null;
        }

        public bool IsRegistered(string name)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));
            return false;
        }

        public void SetAsUnactual(string name, Guid id)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));
        }
    }
}
