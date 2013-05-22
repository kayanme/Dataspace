using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;


namespace Resources.Test.TestResources
{
    [Export(typeof(ResourcePoster))]
    public class ElementPoster : ResourcePoster<Element>
    {
#pragma warning disable 0649
        [Import]
        private ResourcePool _pool;
#pragma warning restore 0649
        protected override void WriteResourceTyped(Guid key, Element resource)
        {
            if (_pool.Elements.ContainsKey(key))
               _pool.Elements[key] = resource;
            else            
               _pool.Elements.Add(key,resource);
            
        }

        protected override void DeleteResourceTyped(Guid key)
        {
            if (_pool.Elements.ContainsKey(key))
                _pool.Elements.Remove(key);
            
        }


    }
}
