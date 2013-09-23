using System;
using System.Diagnostics.Contracts;
using Dataspace.Common.Announcements;
using Dataspace.Common.ClassesForImplementation;

namespace Dataspace.Common.Interfaces
{
    /// <summary>
    /// Управление подписками на обновление
    /// </summary>
    [ContractClass(typeof(IAnnouncerSubscriptorContract))]
    public interface IAnnouncerSubscriptor
    {
        /// <summary>
        /// Подписка на обновление конкретного ресурса.
        /// </summary>
        /// <param name="resourceName">Название ресурса.</param>
        /// <param name="id">Ключ ресурса.</param>
        /// <remarks> Применяется для разрешения публикации события об изменении конкретного ресурса в связи "вверх-вниз".<see cref="AnnouncerUplink"/>Очередь событий</remarks>
        void SubscribeForResourceChangePropagation(string resourceName, Guid id);

        /// <summary>
        /// Отписка от рассылки обновления ресурса.
        /// </summary>
        /// <param name="resourceName">Название ресурса.</param>
        /// <param name="id">Ключ ресурса.</param>
        /// <remarks> Применяется для запрета публикации события об изменении конкретного ресурса в связи "вверх-вниз".
        /// Автоматически применяется при апдейте ресурса.
        /// <see cref="AnnouncerUplink"/>Очередь событий</remarks>
        void UnsubscribeForResourceChangePropagation(string resourceName, Guid id);

        /// <summary>
        /// Подписка на обновление ресурса заданного типа.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <returns>Маркер подписки</returns>
        SubscriptionToken SubscribeForResourceChange<T>();

        /// <summary>
        /// Отписывается от обновления ресурса.
        /// </summary>
        /// <param name="token">Маркер обновления.</param>
        void UnsubscribeForResourceChange(SubscriptionToken token);

        /// <summary>
        /// Подписка на рассылку обновлений ресурса.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <remarks> Применяется для разрешения публикации события об изменении конкретного ресурса в связи "вверх-вниз".<see cref="AnnouncerUplink"/>Очередь событий</remarks>
        SubscriptionToken SubscribeForResourceChangePropagation<T>();

        /// <summary>
        /// Отписывается от рассылки обновления ресурса.
        /// </summary>
        /// <param name="token">Маркер обновления.</param>
        void UnsubscribeForResourceChangePropagation(SubscriptionToken token);

    }

    [ContractClassFor(typeof(IAnnouncerSubscriptor))]
    internal abstract class IAnnouncerSubscriptorContract : IAnnouncerSubscriptor
    {

        
    
        public void SubscribeForResourceChangePropagation(string resourceName, Guid id)
        {
            throw new NotImplementedException();
        }

        public void UnsubscribeForResourceChangePropagation(string resourceName, Guid id)
        {
            Contract.Requires(!string.IsNullOrEmpty(resourceName));
        }

        public SubscriptionToken SubscribeForResourceChange<T>()
        {
            Contract.Ensures(Contract.Result<SubscriptionToken>() != null);
            return null;
        }

        public void UnsubscribeForResourceChange(SubscriptionToken token)
        {
            Contract.Requires(token != null);
        }

        public SubscriptionToken SubscribeForResourceChangePropagation<T>()
        {
            throw new NotImplementedException();
        }

        public void UnsubscribeForResourceChangePropagation(SubscriptionToken token)
        {
            throw new NotImplementedException();
        }
    }
}
