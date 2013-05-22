using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Common.ParsingServices;

namespace Dataspace.Common.ParsingServices
{
    [Export(typeof(Parser<bool>))]
    internal sealed class BoolParser : Parser<bool>
    {
        public override string FormatSpecifier
        {
            get { return ""; }
        }

        protected internal override bool IsAppropriateForParsing(string source, string[] args)
        {
            return ReturnSpecifier(source) == "";
        }

        protected override bool IsValid(string source)
        {
            bool d;
            return bool.TryParse(source, out d);
        }

        public override bool Parse(string source, params object[] args)
        {
            return bool.Parse(source);
        }
    }
}
