using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Announcements;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;
using EmitMapper.MappingConfiguration;

namespace Dataspace.Over.EntityFramework.Examples.Realisations
{

    [Export(typeof(IResourcePosterFactory))]
    internal sealed class EntityPosterFactory:IResourcePosterFactory
    {
        [Import]
        private IGenericPool _pool;

        internal static void WriteNotSave(Guid key, object resource, ResourcesModelContainer model,Type resType)
        {
            var existing = model.Set(resType)
                                .Find(key);
            if (existing == null)
            {
                (resource as BaseEntity).Id = key;
                model.Set(resType).Add(resource);
            }
            else
            {
                EmitMapper.ObjectMapperManager.DefaultInstance 
                                              .GetMapperImpl(resType,resType,new DefaultMapConfig())
                                              .Map(resource, existing,new object());
            }
        }

        internal static void DeleteNoSave(Guid key, ResourcesModelContainer model,Type resType)
        {
            var existing = model.Set(resType)
                                .Find(key);
            if (existing != null)
            {
                model.Set(resType).Remove(existing);
            }
        }

        private class Writer<T>:ResourcePoster<T> where T:class
        {
            

            protected override void WriteResourceTyped(Guid key, T resource)
            {
                Debug.Assert(resource is BaseEntity);
                using (var model = new ResourcesModelContainer())
                {
                    WriteNotSave(key, resource, model,typeof(T));
                    model.SaveChanges();
                }
            }

            protected override void DeleteResourceTyped(Guid key)
            {
             
                using (var model = new ResourcesModelContainer())
                {
                    DeleteNoSave(key,model,typeof(T));   
                    model.SaveChanges();
                }
            }
        }
     

        public ResourcePoster<T> CreateWriter<T>() where T : class
        {
            if (!typeof(BaseEntity).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException();
           return new Writer<T>();
        }

        private void WriteSeria(IEnumerable<Tuple<UnactualResourceContent,object>> objects )
        {
            using (var model = new ResourcesModelContainer())
            {
                foreach(var objs in objects.GroupBy(k=>k.Item1.ResourceName))
                {
                    var type = _pool.GetTypeByName(objs.Key);
                    foreach (var writingObject in objs)
                    {
                        var key = writingObject.Item1.ResourceKey;
                        if (writingObject.Item2 ==null)
                            DeleteNoSave(key,model,type);
                        else
                        {
                            WriteNotSave(key,writingObject.Item2,model,type);
                        }
                    }
                }
                model.SaveChanges();
            }
        }

        public Action<IEnumerable<Tuple<UnactualResourceContent, object>>> ReturnSerialWriter()
        {
            return WriteSeria;
        }
    }
}
