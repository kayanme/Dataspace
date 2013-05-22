using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common;
using Dataspace.Common.Attributes;
using Dataspace.Common.Attributes.CachingPolicies;

namespace Resources.Notification.Test
{
    [Resource("UnnotifiedElement")]
    [Serializable]
    [SimpleCaching]
    public class UnnotifiedElement
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
