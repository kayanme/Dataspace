using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indusoft.Test.MockresourceProviders;
using PerfTest.Metadata;

namespace PerfTest.Classes
{
    public abstract class NodeCommandBase<T>
    {
        public abstract void Do(T service);

        public abstract bool Check(Store store);

        protected ResBase CreateResource(string name)
        {
            return ResourceSpaceDescriptions.ResourceAssembly.CreateInstance(name) as ResBase;
        }

        protected Type GetResourceType(string name)
        {
            return ResourceSpaceDescriptions.ResourceAssembly.GetType(name);
        }
    }
}
