using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Common.ParsingServices
{
    [Export(typeof(Parser<DateTime>))]
    internal sealed class DateTimeParser : Parser<DateTime>
    {
        public override string FormatSpecifier
        {
            get { return ""; }
        }

        protected internal override bool IsAppropriateForParsing(string source, string[] args)
        {
            return ReturnSpecifier(source) == "" && source[0] != '[';
        }

        protected override bool IsValid(string source)
        {
            DateTime d;
            return DateTime.TryParse(source,CultureInfo.InvariantCulture,DateTimeStyles.None,out d);
        }

        public override DateTime Parse(string source, params object[] args)
        {
            return DateTime.Parse(source, CultureInfo.InvariantCulture);
        }
    }
}
