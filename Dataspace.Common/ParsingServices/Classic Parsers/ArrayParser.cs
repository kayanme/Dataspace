using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Common.ParsingServices;

namespace Dataspace.Common.ParsingServices.Classic_Parsers
{
   
    internal class ArrayParser<T> : Parser<T[]>
    {
        #region Imports

#pragma warning disable 0649
        [ImportMany]
        private IEnumerable<Parser<T>> _blocks;
#pragma warning restore 0649

        #endregion

        protected internal override bool IsAppropriateForParsing(string source,string[] args)
        {
            var spec = this.ReturnSpecifier(source);
            source = source.Remove(0, spec.Length+1);
            var vals = source.Split(';');
            spec = string.IsNullOrEmpty(spec) ? "" : spec + "$";
            return vals.All(k => _blocks.Any(k2 => k2.IsAppropriateForParsing(spec+k,args)));
        }

        protected override bool IsValid(string source)
        {
            var vals = source.Split(';');
            return vals.All(k => _blocks.Any(k2 => k2.Validate(k)));
        }

        public override T[] Parse(string source, params object[] args)
        {
            var vals = source.Split(';');
            return vals.Select(k => _blocks.First(k2 => k2.Validate(k))
                                           .Parse(k,args))
                                           .ToArray();
        }
    }

    [Export(typeof(Parser<bool[]>))]
    internal sealed class BoolArray : ArrayParser<bool>
    {
    }

    [Export(typeof(Parser<DateTime[]>))]
    internal sealed class DateTimeArray : ArrayParser<DateTime>
    {
    }
}
