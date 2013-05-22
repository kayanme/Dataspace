using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;

using Resources.Security.Test.SecurityResources;
using Resources.Test.TestResources.ElementDeriver;
using Resources.Test.TestResources.ElementInModel;
using Resources.Test.TestResources.SecurityResource;

namespace Resources.Test.TestResources
{
    [Export(typeof(ResourceRegistrator))]
    public class Registrator:ResourceRegistrator
    {
        protected override Type[] ResourceTypes
        {
            get { return new[] { typeof(Model), typeof(Element),
                typeof(ElementDeriver.ElementDeriver),
                typeof(ModelDeriver.ModelDeriver), 
                typeof(SecurityPermissions),
                typeof(SecurityGroup),
                typeof(ElementsInModel) }; }
        }
    }
}
