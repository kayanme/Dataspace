using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.ClassesForImplementation.Security;
using Dataspace.Common.Security;
using Resources.Security.Test;

namespace Resources.Test.TestResources.SecurityResource
{

    [Export(typeof(SecurityProvider))]
    internal class ModelSecurityProvider:SecurityProvider<Model,SecurityContext>
    {
        [ThreadStatic]
        public static int TimesWasUsed = 0;

        protected override IEnumerable<KeyValuePair<Guid, SecurityToken>> GetTokensFor(IEnumerable<Guid> ids, SecurityContext context)
        {
            TimesWasUsed++;
            return ids.ToDictionary(k => k, k => GetToken(k, context));
        }

        private SecurityToken GetToken(Guid id, SecurityContext context)
        {           
            var permissions = Cachier.Get<SecurityPermissions>(id);
            bool canRead = (context.BelongingGroups.Any(k => permissions.AllowedForRead.Any(k2 => k2 == k)));
            bool canWrite = (context.BelongingGroups.Any(k => permissions.AllowedForWrite.Any(k2 => k2 == k)));
            return CreateToken(canRead, canWrite);
        }

        protected override SecurityToken GetTokenFor(Guid id, SecurityContext context)
        {
            TimesWasUsed++;
            return GetToken(id,context);
        }
    }
}
