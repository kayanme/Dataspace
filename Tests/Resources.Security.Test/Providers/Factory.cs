using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;


namespace Resources.Test.Providers
{
    [Export(typeof(IResourceGetterFactory))]
    internal class Factory:IResourceGetterFactory
    {
        public ResourceGetter<T> CreateGetter<T>() where T : class
        {
            throw new NotImplementedException();
        }
    }
}
