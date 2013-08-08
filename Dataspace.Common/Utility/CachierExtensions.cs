using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Dataspace.Common;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;

namespace Dataspace.Common.Utility
{
    public static class CachierExtensions
    {
        /// <summary>
        /// Получение набора ресурсов по первичному ключу.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <param name="cachier">Кэшер.</param>
        /// <param name="ids">Ключи.</param>
        /// <returns>
        /// Ресурсы
        /// </returns>
        [Pure]
        public static IEnumerable<T> Get<T>(this ITypedPool cachier, IEnumerable<Guid> ids) where T : class
        {
            return ids.Select(cachier.GetLater<T>).ToArray().Select(k => k.Value).ToArray();
        }


        /// <summary>
        /// Получение набора ресурсов по первичному ключу.
        /// </summary>
        /// <param name="cachier">Кэшер.</param>
        /// <param name="name">Имя ресурса</param>
        /// <param name="ids">Ключи.</param>
        /// <returns>
        /// Ресурсы
        /// </returns>
        [Pure]
        public static IEnumerable<object> Get(this IGenericPool cachier,string name, IEnumerable<Guid> ids) 
        {
            return ids.Select(k=>cachier.GetLater(name,k)).ToArray().Select(k => k.Value).ToArray();
        }

        /// <summary>
        /// Получение набора ресурсов по динамическому.
        /// </summary>
        /// <param name="cachier">Кэшер.</param>    
        /// <param name="query">Запрос.</param>
        /// <returns>
        /// Ресурсы
        /// </returns>
        [Pure]
        public static IEnumerable<T> FindFilled<T>(this ITypedPool cachier,  object query) where T:class
        {
            return cachier.Get<T>(cachier.Find<T>(query));
        }

        /// <summary>
        /// Получение набора ресурсов по запросу.
        /// </summary>
        /// <param name="cachier">Кэшер.</param>
        /// <param name="name">Имя ресурса </param>
        /// <param name="query">Запрос.</param>
        /// <returns>
        /// Ресурсы
        /// </returns>
        [Pure]
        public static IEnumerable<object> QueryFilled(this IGenericPool cachier,string name, string query) 
        {
            return cachier.Get(name,cachier.Query(name,new UriQuery(query)));
        }


        /// <summary>
        /// Получение набора ресурсов по запросу.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <param name="cachier">Кэшер.</param>
        /// <param name="query">Запрос.</param>
        /// <returns>
        /// Ресурсы
        /// </returns>
        [Pure]
        public static IEnumerable<T> QueryFilled<T>(this ITypedPool cachier, string query) where T : class
        {
            return cachier.Get<T>(cachier.Query<T>(query));
        }
		/// <summary>
		/// Получение набора ресурсов по запросу.
		/// </summary>
		/// <typeparam name="T">Тип ресурса</typeparam>
		/// <param name="cachier">Кэшер.</param>
		/// <param name="query">Запрос.</param>
		/// <returns>
		/// Ресурсы
		/// </returns>
		[Pure]
		public static IEnumerable<T> QueryFilled<T>(this ITypedPool cachier, UriQuery query) where T : class
		{
			return cachier.Get<T>(cachier.Query<T>(query));
		}


        /// <summary>
        /// Получение набора ресурсов по запросу.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <param name="cachier">Кэшер.</param>
        /// <param name="query">Запрос.</param>
        /// <returns>
        /// Ресурсы
        /// </returns>
        [Pure]
        public static IEnumerable<T> GetFilled<T>(this ITypedPool cachier, Func<IEnumerable<Guid>> query) where T : class
        {
            return cachier.Get<T>(query());
        }

        /// <summary>
        /// Получение набора ресурсов по первичному ключу родительского элемента.
        /// </summary>
        /// <typeparam name="TResource">Тип ресурса</typeparam>
        /// <typeparam name="TLink">Тип родителя.</typeparam>
        /// <param name="cachier">Кэшер.</param>
        /// <param name="id">Ключ условного родителя.</param>
        /// <returns>
        /// Ресурсы
        /// </returns>
        [Pure]
        public static IEnumerable<Guid> Get<TResource, TLink>(this ITypedPool cachier, Guid id) where TResource : class
        {
            var query = cachier.Spec;
            query[(cachier as IGenericPool).GetNameByType(typeof (TLink))] = id;
            return cachier.Find<TResource>(query as object);
        }

        /// <summary>
        /// Получение набора ресурсов по первичному ключу родительского элемента.
        /// </summary>
        /// <typeparam name="TResource">Тип ресурса</typeparam>
        /// <typeparam name="TLink">Тип родителя.</typeparam>
        /// <param name="cachier">Кэшер.</param>
        /// <param name="id">Ключ условного родителя.</param>
        /// <returns>
        /// Ресурсы
        /// </returns>
        [Pure]
        public static IEnumerable<TResource> GetFilled<TResource, TLink>(this ITypedPool cachier, Guid id) where TResource : class
        {
            return cachier.Get<TResource>(cachier.Get<TResource,TLink>(id));
        }

        /// <summary>
        /// Gets the single dependent resource.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <param name="cachier">Кэшер.</param>
        /// <param name="query">Запрос.</param>
        /// <returns>
        /// Единственный ресурс, null, если такового нет.
        /// </returns>
        /// <exception cref="InvalidOperationException">Если ресурсов по запросу больше 1.</exception>
        [Pure]
        public static T GetOneDependent<T>(this ITypedPool cachier,UriQuery query) where T : class
        {
            return cachier.Query<T>(query).SingleOrDefault().ByDefault(cachier.Get<T>,null);
        }


        /// <summary>
        /// Получает ресурс по его описанию.
        /// </summary>       
        /// <param name="cachier">Кэшер.</param>
        /// <param name="resource">Описание ресурса.</param>
        /// <returns>
        /// Единственный ресурс, null, если такового нет.
        /// </returns> 
        [Pure]
        public static object Get(this IGenericPool cachier, ResourceDescription resource) 
        {
            return cachier.Get(resource.ResourceName, resource.ResourceKey);
        }

       
    }
}
