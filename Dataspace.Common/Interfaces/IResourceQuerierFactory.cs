using Dataspace.Common.ClassesForImplementation;

namespace Dataspace.Common.Interfaces
{
    /// <summary>
    /// Фабрика запросчиков ресурсов по умолчанию.
    /// </summary>
    public interface IResourceQuerierFactory
    {

        /// <summary>
        /// Создает запросчик для ресурса.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        ResourceQuerier<T> CreateQuerier<T>() where T : class;
    }
}
