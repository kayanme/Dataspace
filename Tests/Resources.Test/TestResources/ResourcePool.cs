using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Resources.Test.TestResources
{
    internal class ResourcePool
    {
        internal Dictionary<Guid, Model> Models = new Dictionary<Guid, Model>();
        internal Dictionary<Guid, Element> Elements = new Dictionary<Guid, Element>();
    }
}
