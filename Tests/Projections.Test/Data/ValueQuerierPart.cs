using System;
using System.Collections.Generic;
using System.Linq;
using Dataspace.Common;
using Dataspace.Common.Attributes;


namespace Projections.Test.Data
{
    partial class ValueQuerier
    {

        [IsQuery]
        public IEnumerable<Guid> ByAttribute(Guid attribute)
        {
            return _queryProc.GetItems<Value>(k => k.AttributeId == attribute)
                              .Select(k => k.Id).ToArray();
        }

        [IsQuery]
        [QueryNamespace("http://tempuri.org/Namefilter")]
        [QueryNamespace("http://tempuri.org/NamefilterWithGroups")]
        public IEnumerable<Guid> ByAttributeWithName(Guid attribute)
        {
            return _queryProc.GetItems<Value>(k => k.AttributeId == attribute
                                                    && k.Name.StartsWith("Allowed"))
                              .Select(k => k.Id).ToArray();
        }

        [IsQuery]
        [QueryNamespace("http://tempuri.org/Namefilter")]
        public IEnumerable<Guid> ByAttributeWithCustomName(Guid attribute,string name)
        {
            return _queryProc.GetItems<Value>(k => k.AttributeId == attribute
                                                    && k.Name.StartsWith(name))
                              .Select(k => k.Id).ToArray();
        }

        [IsQuery]
        [QueryNamespace("http://tempuri.org/NamefilterWithGroups")]
        public IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>> ByAttributeWithCustomName([Resource("Attribute")]IEnumerable<Guid> attribute, string name)
        {
            return attribute.ToDictionary(k => k, k => ByAttributeWithCustomName(k, name));
        }

        [IsQuery]        
        public IEnumerable<Guid> ByElement(Guid element)
        {
         
            return _queryProc.GetItems<Attribute>(k => k.ElementId == element)
                             .SelectMany(k=>_queryProc.GetItems<Value>(k3=>k3.AttributeId == k.Id))
                             .Select(k => k.Id).ToArray();
        }

    }
}
