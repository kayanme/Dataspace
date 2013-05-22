using System;
using System.Runtime.Serialization;
using Dataspace.Common.Data;

namespace Dataspace.Common.Announcements
{
    /// <summary>
    /// Описание обновления безопасности
    /// </summary>
    [Serializable]
    [DataContract]
    public sealed class SecurityUpdate : ResourceDescription
    {

        /// <summary>
        /// Получает значение, показывающее, является ли апдейт глобальным для всего типа.
        /// </summary>
        /// <value>
        ///   <c>true</c> if для всего типа; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool UpdateAll { get; set; }
    }
}
