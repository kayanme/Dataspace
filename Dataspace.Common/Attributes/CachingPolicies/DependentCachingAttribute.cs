using System;
using System.Diagnostics;

namespace Dataspace.Common.Attributes.CachingPolicies
{
    /// <summary>
    /// Обновление в зависимости от изменения другого объекта
    /// </summary>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public class DependentCachingAttribute : CachingPolicyAttribute
    {

        public Type ParentType { get; private set; }

        public DependentCachingAttribute(Type parentType)
        {
           var correctType = IsDefined(parentType, typeof (ResourceAttribute)) || IsDefined(parentType, typeof (CachingDataAttribute));

           Debug.Assert(correctType, "Родительский тип не является ресурсом");
           if (!correctType)
                throw new ArgumentException("Родительский тип не является ресурсом");

           ParentType = parentType;
        }
    }
}
