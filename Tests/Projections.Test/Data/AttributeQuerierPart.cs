using System;
using System.Collections.Generic;
using System.Linq;
using Dataspace.Common;
using Dataspace.Common.Attributes;


namespace Projections.Test.Data
{
    partial class AttributeQuerier
    {
        [IsQuery]
        public IEnumerable<Guid> ByElement(Guid element)
        {
           return  _queryProc.GetItems<Attribute>(k => k.ElementId == element)
                             .Select(k => k.Id).ToArray();
        }

        [IsQuery]
        [QueryNamespace("http://tempuri.org/Namefilter")]
        [QueryNamespace("http://tempuri.org/NamefilterWithGroups")]
        public IEnumerable<Guid> ByElementWithName(Guid element)
        {
            return _queryProc.GetItems<Attribute>(k => k.ElementId == element 
                                                    && k.Name.StartsWith("Allowed"))
                              .Select(k => k.Id).ToArray();
        }

        [IsQuery]
        [QueryNamespace("http://tempuri.org/Namefilter")]
        public IEnumerable<Guid> ByElementWithCustomName(Guid element,string name)
        {
            return _queryProc.GetItems<Attribute>(k => k.ElementId == element
                                                    && k.Name.StartsWith(name))
                              .Select(k => k.Id).ToArray();
        }

        [IsQuery]
        [QueryNamespace("http://tempuri.org/NamefilterWithGroups")]
        public IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>> ByElementWithCustomName([Resource("Element")]IEnumerable<Guid> element, string name)
        {
            return element.ToDictionary(k=>k,k => ByElementWithCustomName(k, name));
        }

        [IsQuery]
        [QueryNamespace("http://tempuri.org/GroupScheme")]
        public IEnumerable<Guid> ByAttributeGroup(Guid attributeSubGroup)
        {
            return _queryProc.GetItems<Attribute>(k => k.ElementId == attributeSubGroup)
                              .Select(k => k.Id).ToArray();
        }
    }
}
