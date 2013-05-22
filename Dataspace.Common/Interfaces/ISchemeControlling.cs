using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Schema;

namespace Dataspace.Common.Interfaces
{
    /// <summary>
    /// Дает возможность добавлять и удалять схемы.
    /// </summary>
    public interface ISchemeControlling
    {
        /// <summary>
        /// Добавляет новые схемы.
        /// </summary>
        /// <param name="schema">Схемы.</param>
         void IntegrateNewSchemas(IEnumerable<XmlSchema> schema);

         /// <summary>
         /// Удаляет схему.
         /// </summary>
         /// <param name="space">Пространство имен схемы.</param>
         void RemoveSchema(string space);

         /// <summary>
         /// Получает все схемы.
         /// </summary>
         /// <returns>Набор схем</returns>
         XmlSchemaSet GetSchemas();
    }
}
