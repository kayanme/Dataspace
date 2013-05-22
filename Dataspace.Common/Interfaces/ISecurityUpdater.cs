using System;
using Dataspace.Common.Security;

namespace Dataspace.Common.Interfaces
{
    /// <summary>
    /// Менеджер безопасности
    /// </summary>
    public interface ISecurityManager
    {
        /// <summary>
        /// Обновить настройки безопасности для экземпляра определенного типа.
        /// </summary>
        /// <param name="type">Тип.</param>
        /// <param name="id">Ключ.</param>
        void UpdateSecurity(Type type, Guid id);

        /// <summary>
        /// Обновить настройки безопасности для всех экземпляров типа.
        /// </summary>
        /// <param name="type">The type.</param>
        void UpdateSecurity(Type type);

        /// <summary>
        /// Обновить все настройки безопасности.
        /// </summary>
        void UpdateSecurity();

        /// <summary>
        /// Получить разрешения на экземпляр объекта.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <param name="id">Ключ.</param>
        /// <returns>Разрешения</returns>
        SecurityToken GetToken<T>(Guid id);

        /// <summary>
        /// Получить разрешения на экземпляр объекта.
        /// </summary>
        /// <param name="name"> Название ресурса</param>
        /// <param name="id">Ключ.</param>
        /// <returns>Разрешения</returns>
        SecurityToken GetToken(string name, Guid id);

        /// <summary>
        /// Получить разрешения на экземпляр объекта отложенным способом.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <param name="id">Ключ.</param>
        /// <returns>Разрешения</returns>
        Lazy<SecurityToken> GetTokenLater<T>(Guid id);

        /// <summary>
        /// Получить разрешения на экземпляр объекта отложенным способом.
        /// </summary>
        /// <param name="name"> Название ресурса</param>
        /// <param name="id">Ключ.</param>
        /// <returns>Разрешения</returns>
        Lazy<SecurityToken> GetTokenLater(string name, Guid id);
    }
}
