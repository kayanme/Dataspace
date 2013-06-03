using System;
using System.ComponentModel.Composition;
using Dataspace.Common.ClassesForImplementation.Security;

namespace Indusoft.Testhelper.Defaults
{
    [Export(typeof(SecurityContextFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SecContextFactor : SecurityContextFactory
    {
        private class Cont:ISecurityContext
        {
            public Guid SessionCode
            {
                get { return Guid.Empty; }
            }
        }

        public override ISecurityContext GetContext()
        {
            return new Cont();
            
        }
    }
}
