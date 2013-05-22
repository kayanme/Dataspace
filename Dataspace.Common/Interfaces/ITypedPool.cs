using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Security;
using Dataspace.Common.Data;

namespace Dataspace.Common.Interfaces
{
    /// <summary>
    /// Интерфейс службы получения объектов (ресурсов) из базы данных/сервера с промежуточным кэшированием.
    /// </summary>
    [ContractClass(typeof(TypedPoolContracts))]
    public interface ITypedPool
    {
        /// <summary>
        /// Получение ресурса по первичному ключу.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <param name="id">Ключ.</param>     
        /// <returns>Ресурс; null, если ресурс отсутсвует</returns>    
        [Pure]
        T Get<T>(Guid id) where T : class;

        /// <summary>
        /// Получение ключей ресурсов по запросу.
        /// </summary>
        /// <typeparam name="T">Тип.</typeparam>
        /// <param name="query">Запрос.</param>
        /// <param name="nmspace">Неймспейс</param>
        /// <returns>Ресурс.</returns>
        [Pure]
        IEnumerable<Guid> Get<T>(UriQuery query, string nmspace = "") where T : class;

        /// <summary>
        /// Получение ключей ресурсов по запросу.
        /// </summary>
        /// <typeparam name="T">Тип.</typeparam>
        /// <param name="query">Запрос.</param>
        /// <returns>Ресурс.</returns>
        [Pure]
        IEnumerable<Guid> Get<T>(string query) where T : class;

        /// <summary>
        /// Позволяет делаеть отложенную загрузку ресурса. Отложенная загрузка позволяет накапливать серии данных,
        /// получаемые только в требуемый момент.
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="id">Ключ.</param>
        /// <returns>Отложенное значение</returns>      
        [Pure]
        Lazy<T> GetLater<T>(Guid id) where T : class;

        /// <summary>
        /// Записывает ресурс.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <param name="id">Ключ ресурса.</param>
        /// <param name="resource">Ресурс.</param>
        /// <exception cref="SecurityException">Если запись недопустима из-за настроек безопасности</exception>
        void Post<T>(Guid id, T resource);

        /// <summary>
        /// Помечает ресурс как неактуальный.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <param name="id">Ключ ресурса.</param>
        void SetAsUnactual<T>(Guid id);

        /// <summary>
        /// Получает ресурсы, используя альтернативный синтаксис.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <param name="query">Запрос </param>
        /// <param name="namespc">Пространство имен запроса.</param>
        /// <returns>Функция для запроса, принимающая именованые аргументы вида (name:"")</returns>
        IEnumerable<Guid> Find<T>(object query,string namespc = "");

        dynamic Query { get; } 
    }

    [ContractClassFor(typeof(ITypedPool))]
    internal abstract class TypedPoolContracts : ITypedPool
    {
        public T Get<T>(Guid id) where T : class
        {
             return default(T);
        }

        public IEnumerable<Guid> Get<T>(UriQuery query, string nmspace) where T : class
        {
            Contract.Requires(query != null);
            Contract.Ensures(Contract.Result<IEnumerable<Guid>>() != null);
            return new Guid[0];
        }

        public IEnumerable<Guid> Get<T>(string query) where T : class
        {
            Contract.Requires(query != null);
            Contract.Ensures(Contract.Result<IEnumerable<Guid>>() != null);
            return new Guid[0];
        }

        public Lazy<T> GetLater<T>(Guid id) where T : class
        {            
            Contract.Ensures(Contract.Result<Lazy<T>>() != null);
            return null;
        }

        public void Post<T>(Guid id, T resource)
        {         
        }

        public void SetAsUnactual<T>(Guid id)
        {            
        }

        public IEnumerable<Guid> Find<T>(object query, string namespc = "")
        {
            return null;
        }


        public dynamic Query { get; private set; }
    }

   
}
