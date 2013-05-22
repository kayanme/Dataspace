using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Common.ParsingServices
{
    [Export(typeof(Parser<string>))]
    internal sealed class StringParser:Parser<string>
    {
        public override string FormatSpecifier
        {
            get { return ""; }
        }


        protected internal override bool IsAppropriateForParsing(string source,string[] args)
        {
            return true;
        }

        protected override bool IsValid(string source)
        {
            return true;
        }

        public override string Parse(string source, params object[] args)
        {        
            return source;
        }
    }
}
