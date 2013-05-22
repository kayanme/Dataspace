using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.ClassesForImplementation.Security;
using Dataspace.Common.Interfaces;
using Resources.Security.Test.SecurityResources;

namespace Resources.Security.Test
{
    [Export(typeof(SecurityContextFactory))]
    internal class TestSecurityContextFactory:SecurityContextFactory
    {
      

        public override ISecurityContext GetContext()
        {
            var id = Session.UserId;
            var context = new SecurityContext {SessionCode = id};
            var groups = Cachier.Get<SecurityGroup>(id);
            context.BelongingGroups = groups.Groups;
            return context;
        }
    }
}
