using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.ClassesForImplementation.Security;
using Dataspace.Common.Interfaces;
using Resources.Test.TestResources.SecurityResource;

namespace Resources.Security.Test.SecurityResources
{
    [Export(typeof(IDefaultSecurityProvider))]
    internal class DefaultSecurityProvider:IDefaultSecurityProvider
    {
        public SecurityProvider CreateProvider<T>()
        {
            return new ModelSecurityProvider();
        }
    }
}
