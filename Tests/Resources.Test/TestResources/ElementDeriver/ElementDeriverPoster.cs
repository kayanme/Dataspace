using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;


namespace Resources.Test.TestResources.ElementDeriver
{

    [Export(typeof(ResourcePoster))]
    public class ElementDeriverPoster : ResourcePoster<ElementDeriver>
    {       
        protected override void WriteResourceTyped(Guid key, ElementDeriver resource)
        {
            var element = TypedPool.Get<Element>(key);
            element.Name = resource.Name;
            TypedPool.Post(key,element);

        }

        protected override void DeleteResourceTyped(Guid key)
        {
            var element = TypedPool.Get<Element>(key);
            element.Name = null;
            TypedPool.Post(key, element);

        }
    }
}


