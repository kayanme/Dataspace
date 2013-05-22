using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.ClassesForImplementation.Security;

namespace Dataspace.Common.Interfaces
{
    public interface IDefaultSecurityProvider
    {
        SecurityProvider CreateProvider<T>();
    }
}
