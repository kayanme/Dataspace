using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;

using Resources.Test.TestResources.ElementDeriver;
using Resources.Test.TestResources.ElementInModel;

namespace Resources.Test.TestResources
{
    [Export(typeof(ResourceRegistrator))]
    public class Registrator:ResourceRegistrator
    {
        protected override Type[] ResourceTypes
        {
            get { return new[] { typeof(Model), typeof(Element), typeof(ElementDeriver.ElementDeriver), typeof(ModelDeriver.ModelDeriver), typeof(ElementsInModel) }; }
        }
    }
}
