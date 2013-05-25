using System;
using System.Collections.Generic;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;

namespace Dataspace.Common.Interfaces
{
    /// <summary>
    /// Фабрика запросчиков ресурсов по умолчанию.
    /// </summary>
    public interface IResourceQuerierFactory
    {

        /// <summary>
        /// Создает запросчик для ресурса, если не найдено подходящего.
        /// </summary>
        /// <returns></returns>
        FormedQuery CreateQuerier(string type, string nmspc, string[] args);
    }
}
