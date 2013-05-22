using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;


namespace Hierarchies.Test.Data
{
    [Export(typeof(PropertiesProvider))]
    [QueryNamespace("http://tempuri.org/GroupScheme")]
    public class GroupSchemeDataProvider:PropertiesProvider
    {
         [Resource("ElementGroup")]
         public object GetElementGroup(Guid? id, string name)
         {
             if (name == "Name")
                 return "ElementGroup: " + id;

             throw new ArgumentException();
         }

         [Resource("AttributeGroup")]
         public object GetAttributeGroup(Guid? id, string name)
         {
             if (name == "Name")
                 return "AttributeGroup: " + id;

             throw new ArgumentException();
         }

         [Resource("AttributeSubGroup")]
         public object GetAttributeSubGroup(Guid? id, string name)
         {
             if (name == "Name")
                 return "AttributeSubGroup: " + id;

             throw new ArgumentException();
         }
    }
}
