using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;


namespace Resources.Test.TestResources
{
    [Export(typeof(ResourceGetter))]
    public class ModelGetter : ResourceGetter<Model>
    {

        [Import] 
        private ResourcePool _pool;

        protected override Model GetItemTyped(Guid id)
        {
            try
            {
                return _pool.Models[id];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
          
        }

        protected override IEnumerable<KeyValuePair<Guid, Model>> GetItemsTyped(IEnumerable<Guid> id)
        {
            return id.Select(k => new KeyValuePair<Guid, Model>(k, _pool.Models[k])).ToArray();
        }
      
    }
}
