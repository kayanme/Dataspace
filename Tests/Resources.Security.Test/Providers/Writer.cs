using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common;
using Dataspace.Common.Announcements;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;


namespace Resources.Test.Providers
{
    [Export(typeof(IResourcePosterFactory))]
    internal class Writer:IResourcePosterFactory
    {
        public ResourcePoster<T> CreateWriter<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public Action<IEnumerable<Tuple<UnactualResourceContent, object>>> ReturnSerialWriter()
        {
            return null;
        }
    }
}
