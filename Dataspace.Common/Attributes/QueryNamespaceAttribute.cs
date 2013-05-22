using System;

namespace Dataspace.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Class,AllowMultiple = true)]
    public class QueryNamespaceAttribute:Attribute
    {
        public QueryNamespaceAttribute(string nmspc)
        {
            Namespace = nmspc;
        }

        public string Namespace { get; set; }
    }
}
