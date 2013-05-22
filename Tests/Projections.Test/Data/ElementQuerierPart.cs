using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dataspace.Common;
using Dataspace.Common.Attributes;


namespace Projections.Test.Data
{
    partial class ElementQuerier
    {

        [IsQuery]
        [QueryNamespace("http://tempuri.org/Namefilter")]
        public IEnumerable<Guid> ByValue(Guid value)
        {
            return _queryProc.GetItems<Element>(k=>_queryProc.GetItems<Attribute>(k2=>k2.ElementId == k.Id)
                                                             .Any(k2=>_queryProc.GetItems<Value>(k3=>k3.AttributeId == k2.Id && k3.Id == value)
                                                                                .Any()))                           
                              .Select(k => k.Id).ToArray();
        }

        [IsQuery]
        [QueryNamespace("http://tempuri.org/NamefilterWithGroups")]
        public IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>> ByValue([Resource("Value")]IEnumerable<Guid> value)
        {
            return value.ToDictionary(k => k, ByValue);
        }

        [IsQuery]
        [QueryNamespace("http://tempuri.org/GroupScheme")]
        public IEnumerable<Guid> ByGroup(Guid elementGroup)
        {
            
            return new[] {elementGroup};
        }
    }
}
