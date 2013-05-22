using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Testhelper;

namespace Projections.Advanced.Test
{
    internal class Utils
    {
        public static CompositionContainer MakeContainerAndCache(Settings settings,IEnumerable<Type> types)
        {
            var catalog = MockHelper.CatalogForContainer(new[]{typeof (ITypedPool).Assembly}, types);
            var container = new CompositionContainer(catalog);
            container.GetExportedValue<ICacheServicing>().Initialize(settings,container);
            return container;
        }
    }
}
