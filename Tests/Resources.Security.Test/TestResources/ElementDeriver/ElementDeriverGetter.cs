using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;


namespace Resources.Test.TestResources.ElementDeriver
{
    [Export(typeof(ResourceGetter))]
    public class ElementDeriverGetter : ResourceGetter<ElementDeriver>
    {

        [Import]
        private ResourcePool _pool;

        protected override ElementDeriver GetItemTyped(Guid id)
        {
            if (TypedPool.Get<Element>(id) == null)
                return null;
            return new ElementDeriver {Name = TypedPool.Get<Element>(id).Name};

        }

        protected override IEnumerable<KeyValuePair<Guid, ElementDeriver>> GetItemsTyped(IEnumerable<Guid> id)
        {
            return id.Select(TypedPool.Get<Element>)
                     .Where(k=>k!=null)
                     .Select(k => new KeyValuePair<Guid, ElementDeriver>(k.Key,new ElementDeriver {Name = k.Name})).ToArray();
        }
      
    }
}
