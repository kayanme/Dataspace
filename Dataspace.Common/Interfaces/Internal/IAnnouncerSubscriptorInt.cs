using System;

namespace Dataspace.Common.Interfaces.Internal
{
    /// <summary>
    /// Внутренний интерфейс работы с подписками.
    /// </summary>
    internal interface IAnnouncerSubscriptorInt:IAnnouncerSubscriptor
    {
        /// <summary>
        /// Публикует изменение актуальности токена безопасности.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="id">The id.</param>
        void AnnonunceSecurityUnactuality(string resourceName, Guid? id);
        /// <summary>
        /// Публикует изменение актуальности ресурса.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="id">The id.</param>
        void AnnonunceUnactuality(string resourceName, Guid id);
        /// <summary>
        /// Добавляет ресурс как участникасистемы подписок
        /// </summary>
        /// <param name="name">The name.</param>
        void AddResourceName(string name);
        /// <summary>
        /// Очищает все подписки.
        /// </summary>
        void Clear();
    }
}
