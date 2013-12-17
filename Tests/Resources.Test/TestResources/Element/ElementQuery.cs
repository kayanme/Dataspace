using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;


namespace Resources.Test.TestResources
{

    [Export(typeof(ResourceQuerier))]
    public  class ElementQuery:ResourceQuerier<Element>
    {
#pragma warning disable 0649
        [Import]
        private ResourcePool _pool;
#pragma warning restore 0649

        public static bool WasCalledMultiple = false;

        [IsQuery]
        public IEnumerable<Guid> ByModelName(string modelName)
        {

            return _pool.Models.Values.Where(k => k.Name == modelName)
                .Select(k => k.Key)
                .SelectMany(k => _pool.Elements.Where(k2 => k2.Value.Model == k))
                .Select(k=>k.Key)
                .ToArray();

        }

        [IsQuery]
        public IEnumerable<Guid> ByModelId(Guid model)
        {

            return _pool.Models.Values.Where(k => k.Key == model)
                .Select(k => k.Key)
                .SelectMany(k => _pool.Elements.Where(k2 => k2.Value.Model == k))
                .Select(k=>k.Key)
                .ToArray();

        }

        [IsQuery]
        public IEnumerable<KeyValuePair<Guid,IEnumerable<Guid>>> ByModelIdMult(IEnumerable<Guid> model)
        {
            WasCalledMultiple = true;
            return model.ToDictionary(k => k, ByModelId);
        }

       
        public IEnumerable<Guid> ByModelId(string modelName, Guid modelId)
        {

            return _pool.Models.Values.Where(k => k.Key == modelId || k.Name == modelName)
                .Select(k => k.Key)
                .SelectMany(k => _pool.Elements.Where(k2 => k2.Value.Model == k))
                .Select(k => k.Key)
                .ToArray();

        }

        [IsQuery]
        public IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>> ByModelIdMult(IEnumerable<Guid> model, string modelName)
        {
            WasCalledMultiple = true;
            return model.ToDictionary(k => k, k=>ByModelId(modelName,k));
        }

        [IsQuery]
        public IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>> ByModelIdAndElementName(IEnumerable<Guid> model, string elementName)
        {
           WasCalledMultiple = true;
          var res =              
               model.ToDictionary(modelId=>modelId,modelId=>
               _pool.Models.Values.Where(k => k.Key == modelId)
                 .Select(k => k.Key)
                 .SelectMany(k => _pool.Elements.Where(k2 => k2.Value.Model == k && k2.Value.Name == elementName))
                 .Select(k => k.Key)
                 .ToArray().AsEnumerable());
            return res;
        }

        [IsQuery]
        public IEnumerable<Guid> Uni(UriQuery query)
        {

            return new[] {Guid.Empty};

        }

    }
}
