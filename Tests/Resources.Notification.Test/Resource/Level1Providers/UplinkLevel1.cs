using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;

namespace Resources.Notification.Test.Resource.Level1Providers
{

    [Export(typeof(AnnouncerUplink))]
    internal class UplinkLevel1 : AnnouncerUplink
    {
#pragma warning disable 0649
        [Import]
        private TransporterToLevel2 _transporter;
#pragma warning restore 0649


        public override void OnNext(ResourceDescription value)
        {
            _transporter.MarkResource(value);
        }

        public override void OnError(Exception error)
        {

        }

        public override void OnCompleted()
        {

        }
    }

}

