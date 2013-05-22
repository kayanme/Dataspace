using System;

namespace Dataspace.Common.ClassesForImplementation.Security
{
    /// <summary>
    /// Интерфейс контекста безопасности.
    /// </summary>
    public interface ISecurityContext
    {
        Guid SessionCode{ get; }      
    }
}
