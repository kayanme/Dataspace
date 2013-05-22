using System;

namespace Dataspace.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class IsQueryAttribute:System.Attribute
    {
        /// <summary>
        /// Отключает проверку безопасности в запросе
        /// </summary>
        /// <value>
        ///   <c>true</c> если проверка должна быть отключена; иначе, <c>false</c>.
        /// </value>
        /// <remarks>Пока не работает</remarks>
        public bool SecurityTransparent { get; set; }
    }
}
