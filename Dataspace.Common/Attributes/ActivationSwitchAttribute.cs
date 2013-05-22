using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Attributes
{
   
    [MetadataAttribute,AttributeUsage(AttributeTargets.Method|AttributeTargets.Class,AllowMultiple = true)]
    public sealed class ActivationSwitchAttribute:Attribute
    {

        public object Switch { get; private set; }

        public string Name { get; private set; }
        public string Value { get; private set; }

        internal Enum[] CombinedEnums;
        internal IEnumerable<KeyValuePair<string, string>> Configs; 

        public ActivationSwitchAttribute(object swtch)
        {
            Contract.Requires(swtch != null);
            Contract.Ensures(Switch!=null || CombinedEnums != null);
           if (swtch is Enum)
              Switch = swtch as Enum;        
           else
           {
               var t = swtch as IDictionary<string, object>;
               if (t.ContainsKey("Switch"))
                   CombinedEnums = (t["Switch"] as IEnumerable).OfType<Enum>().ToArray();
               else
               {
                   CombinedEnums = new Enum[0];
               }
               if (t.ContainsKey("Name"))
               {
                   Debug.Assert(t["Name"] is IEnumerable);
                   Debug.Assert(t.ContainsKey("Value"));
                   Debug.Assert(t["Value"] is IEnumerable);
                   Configs = 
                   (t["Name"] as IEnumerable).OfType<string>()
                       .Zip((t["Value"] as IEnumerable).OfType<string>(),
                            (k, v) => new KeyValuePair<string, string>(k, v)).ToArray();
               }
               else
               {
                   Configs = new KeyValuePair<string, string>[0];
               }
               Debug.Assert(CombinedEnums != null);
               Debug.Assert(Configs != null);
           }
          
        }

        public ActivationSwitchAttribute(string name,string value)
        {
            Name = name;
            Value = value;
        }
    }
}
