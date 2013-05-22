using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;

namespace Resources.Selector.Test
{
    [CachingData("TestResource")]
    public class TestResource
    {
        public string GetStyle { get; set; }
    }

    [Export(typeof(ResourceRegistrator))]
    public class Registrator:ResourceRegistrator
    {
        protected override Type[] ResourceTypes
        {
            get { return new[] {typeof (TestResource)}; }
        }
    }
}
