using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Attributes
{

    /// <summary>
    /// Помеченный ресурс или кэш-данные будут проверяться на безопасность.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SecuritizedAttribute:Attribute
    {
    }
}
