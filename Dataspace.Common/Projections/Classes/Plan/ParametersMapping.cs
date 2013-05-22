using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Projections.Classes.Plan
{
    internal class ParametersMapping
    {
        private StringDictionary _mapping = new StringDictionary();

       public ParametersMapping(IEnumerable<string> autoMapping )
       {
           foreach(var p in autoMapping)
           {
               _mapping.Add(p,p);
           }
       }

        public ParameterNames ParameterNames
        {
            get
            {
                return new ParameterNames(_mapping.Keys.OfType<string>());
            }
        }

        public string GetQueryParameterNameForArgument(string name)
        {
            return _mapping[name];
        }

        public override bool Equals(object obj)
        {
            var target = obj as ParametersMapping;
            if (target == null)
                return false;

            if (_mapping.Keys.Count != target._mapping.Keys.Count)
                return false;

            var res = _mapping.Keys.OfType<string>().All(k => target._mapping.ContainsKey(k)
                                                              &&
                                                              string.Equals(target._mapping[k], _mapping[k],
                                                                            StringComparison.InvariantCultureIgnoreCase));
            return res;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            unchecked
            {
                foreach (var code in
                        _mapping.OfType<DictionaryEntry>()
                                .Select(k => k.Key.GetHashCode() + k.Value.GetHashCode()))
                {
                    hash += code;
                }
            }
            return hash;
        }
    }
}
