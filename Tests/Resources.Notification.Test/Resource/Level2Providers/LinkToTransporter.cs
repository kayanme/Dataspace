using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Resources.Notification.Test.Resource.Level2Providers
{
    [Export]
    public class LinkToTransporter:MarshalByRefObject
    {
        public TransporterToLevel2 Transporter { get; set; }
    }
}
