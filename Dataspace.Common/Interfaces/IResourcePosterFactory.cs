using System;
using System.Collections.Generic;
using Dataspace.Common.Announcements;
using Dataspace.Common.ClassesForImplementation;

namespace Dataspace.Common.Interfaces
{
    /// <summary>
    /// Фабрика постеров ресурсов (вызывается, если не найден специализированный постер).
    /// </summary>
    public interface IResourcePosterFactory
    {
        /// <summary>
        /// Создает постер.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <returns></returns>
        ResourcePoster<T> CreateWriter<T>() where T : class;
       
    }
}
