using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;

using Resources.Notification.Test;


namespace Resources.Test.TestResources
{
    [Export(typeof(ResourceRegistrator))]
    public class Registrator:ResourceRegistrator
    {
        protected override Type[] ResourceTypes
        {
            get { return new[] { typeof(NotifiedElement), typeof(UnnotifiedElement) }; }
        }
    }
}
