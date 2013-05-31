using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using Resources.Notification.Test;

namespace Resources.Test.TestResources
{
    [Export]
    internal class ResourcePool
    {
        internal Dictionary<Guid, NotifiedElement> NotifiedElements = new Dictionary<Guid, NotifiedElement>();
        internal Dictionary<Guid, UnnotifiedElement> UnnotifiedElements = new Dictionary<Guid, UnnotifiedElement>();
        
    }
}
