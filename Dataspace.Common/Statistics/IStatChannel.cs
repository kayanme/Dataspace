using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.Statistics;

namespace Dataspace.Common.Statistics
{
    internal interface IStatChannel
    {
        void Register(string resourceType);
        void SendMessageAboutOneResource(Guid id, Actions action, TimeSpan length = default(TimeSpan));
        void SendMessageAboutMultipleResources(Guid[] ids, Actions action, TimeSpan length = default(TimeSpan));
    }
}
