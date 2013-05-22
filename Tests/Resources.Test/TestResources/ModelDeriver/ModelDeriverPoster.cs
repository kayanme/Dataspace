using System;
using System.ComponentModel.Composition;
using Dataspace.Common.ClassesForImplementation;


namespace Resources.Test.TestResources.ModelDeriver
{

    [Export(typeof(ResourcePoster))]
    public class ModelDeriverPoster : ResourcePoster<ModelDeriver>
    {
        protected override void WriteResourceTyped(Guid key, ModelDeriver resource)
        {
            var element = TypedPool.Get<Model>(key);
            element.Name = resource.Name;
            TypedPool.Post(key, element);

        }

        protected override void DeleteResourceTyped(Guid key)
        {
            var element = TypedPool.Get<Model>(key);
            element.Name = null;
            TypedPool.Post(key, element);

        }
    }
}


