using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Dataspace.Common.ClassesForImplementation.Security;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Security;

namespace Indusoft.Testhelper.Defaults
{

    [Export(typeof(IDefaultSecurityProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DefaultSecProvider:IDefaultSecurityProvider
    {
        private class Prov<T>: SecurityProvider<T,ISecurityContext>
        {
            protected override IEnumerable<KeyValuePair<Guid, SecurityToken>> GetTokensFor(IEnumerable<Guid> ids, ISecurityContext context)
            {
                return ids.ToDictionary(k => k, k=>GetTokenFor(k,null));
            }

            protected override SecurityToken GetTokenFor(Guid id, ISecurityContext context)
            {
                return CreateToken(true, true);
            }
        }

        public SecurityProvider CreateProvider<T>()
        {
            return new Prov<T>();
        }
    }
}
