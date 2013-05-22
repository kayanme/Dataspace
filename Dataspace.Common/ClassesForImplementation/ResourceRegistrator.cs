using System;

namespace Dataspace.Common.ClassesForImplementation
{
    /// <summary>
    /// Провайдер типов для регистрации в качестве ресурсов.
    /// </summary>
    public abstract class ResourceRegistrator
    {

        internal Type[] ResourceTypesInt { get { return ResourceTypes; } }

        protected  abstract Type[] ResourceTypes { get; }
       
    }
}
