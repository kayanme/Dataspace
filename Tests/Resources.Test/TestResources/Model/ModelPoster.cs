using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;


namespace Resources.Test.TestResources
{
    [Export(typeof(ResourcePoster))]
    public class ModelPoster : ResourcePoster<Model>
    {
#pragma warning disable 0649
        [Import]
        private ResourcePool _pool;
#pragma warning restore 0649
        protected override void WriteResourceTyped(Guid key, Model resource)
        {
            if (_pool.Models.ContainsKey(key))
                _pool.Models[key] = resource;
            else
                _pool.Models.Add(key, resource);

        }

        protected override void DeleteResourceTyped(Guid key)
        {
            if (_pool.Models.ContainsKey(key))
                _pool.Models.Remove(key);

        }
    }
}
