using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Common.ParsingServices;

namespace Dataspace.Common.ParsingServices.Classic_Parsers
{
    [Export(typeof(Parser<Guid>))]
    internal sealed class GuidParser : Parser<Guid>
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
            Guid d;
            return Guid.TryParse(source, out d);
        }

        public override Guid Parse(string source, params object[] args)
        {
            if (string.IsNullOrEmpty(source))
                return Guid.Empty;
            return Guid.Parse(source);
        }
    }
}
