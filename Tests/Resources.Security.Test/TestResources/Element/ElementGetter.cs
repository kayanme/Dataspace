using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;


namespace Resources.Test.TestResources
{
    [Export(typeof(ResourceGetter))]
    public class ElementGetter:ResourceGetter<Element>
    {

        [Import]
        private ResourcePool _pool;

        public static bool WasChanged = false;

        protected override Element GetItemTyped(Guid id)
        {
            try
            {
                WasChanged = true;
                return _pool.Elements[id];
            }
            catch (KeyNotFoundException)
            {

                return null;
                
            }
           
        }

        protected override IEnumerable<KeyValuePair<Guid, Element>> GetItemsTyped(IEnumerable<Guid> id)
        {
            WasChanged = true;
            return id.Select(k => new KeyValuePair<Guid, Element>(k, _pool.Elements[k])).ToArray();
        }

      
    }
}
