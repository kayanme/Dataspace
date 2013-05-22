﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;


namespace Resources.Test.TestResources
{
    [Export(typeof(ResourceQuerier))]
    public class ModelQuery : ResourceQuerier<Model>
    {
#pragma warning disable 0649
        [Import]
        private ResourcePool _pool;
#pragma warning restore 0649
        [IsQuery]
        public IEnumerable<Guid> GetItemTyped(string element)
        {

            return _pool.Elements.Values.Where(k => k.Name == element)
                .Select(k => k.Key)
                .SelectMany(k => _pool.Models.Where(k2 => k2.Value.Element == k))
                .Select(k=>k.Key)
                .ToArray();

        }
    }
}
