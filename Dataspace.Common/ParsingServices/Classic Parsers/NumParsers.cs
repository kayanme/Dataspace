using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Common.ParsingServices.Classic_Parsers
{

    [Export(typeof(Parser<short>))]
    internal sealed class ShortParser : Parser<short>
    {
        public override string FormatSpecifier
        {
            get { return ""; }
        }

        protected internal override bool IsAppropriateForParsing(string source,string[] args)
        {
            return ReturnSpecifier(source) == "";
        }

        protected override bool IsValid(string source)
        {
            short d;
            return short.TryParse(source, out d);
        }

        public override short Parse(string source, params object[] args)
        {
            return short.Parse(source);
        }
    }

    [Export(typeof(Parser<int>))]
    internal sealed class IntParser : Parser<int>
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
            int d;
            return int.TryParse(source, out d);
        }

        public override int Parse(string source, params object[] args)
        {
            return int.Parse(source);
        }
    }

    [Export(typeof(Parser<long>))]
    internal sealed class LongParser : Parser<long>
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
            long d;
            return long.TryParse(source, out d);
        }

        public override long Parse(string source, params object[] args)
        {
            return long.Parse(source);
        }
    }


    [Export(typeof(Parser<double>))]
    internal sealed class DoubleParser : Parser<double>
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
            double d;
            return double.TryParse(source, out d);
        }

        public override double Parse(string source, params object[] args)
        {
            return double.Parse(source);
        }
    }
}
