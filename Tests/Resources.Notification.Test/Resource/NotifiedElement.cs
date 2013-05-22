using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common;
using Dataspace.Common.Attributes;
using Dataspace.Common.Attributes.CachingPolicies;

namespace Resources.Notification.Test
{
    [Resource("NotifiedElement")]
    [Serializable]
    [SimpleCaching]
    public class NotifiedElement
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
