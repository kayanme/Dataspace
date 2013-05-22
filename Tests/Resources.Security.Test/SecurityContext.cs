using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.ClassesForImplementation.Security;

namespace Resources.Security.Test
{
    public sealed class SecurityContext:ISecurityContext
    {
        public Guid SessionCode
        {
            get; set;
        }

        public IEnumerable<Guid> BelongingGroups { get; set; }
    }
}
