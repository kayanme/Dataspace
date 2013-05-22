using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Dataspace.Common.ClassesForImplementation;


namespace Resources.Test.TestResources.ModelDeriver
{
    [Export(typeof(ResourceGetter))]
    public class ModelDeriverGetter : ResourceGetter<ModelDeriver>
    {

        protected override ModelDeriver GetItemTyped(Guid id)
        {
            if (TypedPool.Get<Model>(id) == null)
                return null;
            return new ModelDeriver { Name = TypedPool.Get<Model>(id).Name };

        }

        protected override IEnumerable<KeyValuePair<Guid, ModelDeriver>> GetItemsTyped(IEnumerable<Guid> id)
        {
            return id.Select(TypedPool.Get<Model>)
                     .Where(k => k != null)
                     .Select(k => new KeyValuePair<Guid, ModelDeriver>(k.Key, new ModelDeriver { Name = k.Name })).ToArray();
        }
      
    }
}
