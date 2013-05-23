using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;

namespace Dataspace.Over.EntityFramework.Examples.Realisations
{
    [Export(typeof(IResourceGetterFactory))]
    internal sealed class EntityGetterFactory:IResourceGetterFactory
    {
        
        private class Getter<T>:ResourceGetter<T> where T:class
        {
            protected override T GetItemTyped(Guid id)
            {
                using (var m = new ResourcesModelContainer())
                {
                    return m.Set<T>().OfType<BaseEntity>().FirstOrDefault(k => k.Id == id) as T;
                }
            }

            protected override IEnumerable<KeyValuePair<Guid, T>> GetItemsTyped(IEnumerable<Guid> id)
            {
                using (var m = new ResourcesModelContainer())
                {
                    return m.Set<T>().OfType<BaseEntity>().Where(k =>id.Contains(k.Id)).ToDictionary(k=>k.Id,k=>k as T);
                }
            }
            
        }

        public ResourceGetter<T> CreateGetter<T>() where T : class
        {
            if (!typeof(BaseEntity).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException();
            return new Getter<T>();
        }
    }
}
