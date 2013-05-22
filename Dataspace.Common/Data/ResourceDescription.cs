using System;
using System.Runtime.Serialization;
using Dataspace.Common.Announcements;

namespace Dataspace.Common.Data
{

    /// <summary>
    /// Описание ресурса.
    /// </summary>
    [DataContract]
    [Serializable]
    [KnownType(typeof(UnactualResourceContent ))]
    [KnownType(typeof(SecurityUpdate))]
    public class ResourceDescription
    {
        /// <summary>
        /// Имя типа ресурса.
        /// </summary>
        /// <value>
        /// Имя типа
        /// </value>
        [DataMember]
        public string ResourceName { get; set; }

        /// <summary>
        /// Ключ ресурса.
        /// </summary>
        /// <value>
        /// Ключ.
        /// </value>
        [DataMember]
        public Guid ResourceKey { get; set; }
    }
}
