using System;

namespace Dataspace.Common.Attributes
{

    /// <summary>
    /// Помечает класс как участника экосистемы ресурсов.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ResourceAttribute : System.Attribute
    {
        /// <summary>
        /// Строковое имя ресурса.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }    

        public ResourceAttribute(string name)
        {
            Name = name;          
        }
    }
}
