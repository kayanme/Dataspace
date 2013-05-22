using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;

namespace Projections.Test.Data
{
    [Export(typeof(PropertiesProvider))]
    [QueryNamespace("http://tempuri.org/BaseScheme")]
    internal class BaseSchemeDataProvider : PropertiesProvider
    {

        [Resource("Code")]
        public object GetElementGroup(Guid? id, string name)
        {
            if (name == "Value")
                return id;

            throw new ArgumentException();
        }
    }
}
