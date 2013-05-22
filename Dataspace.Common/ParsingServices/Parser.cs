using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Common.ParsingServices
{
   
    public abstract class Parser<T>
    {
        public virtual string FormatSpecifier { get { return ""; } }
        protected internal abstract bool IsAppropriateForParsing(string source,string[] args);
        protected abstract bool IsValid(string source);
        public abstract T Parse(string source,params object[] args);

        public bool Validate(string source)
        {
            return IsValid(source);
        }

        protected internal string ReturnSpecifier(string s)
        {
            return s.Contains("$") ? s.Split('$')[0] : "";
        }

        protected bool IsSpecified(string s)
        {
            var sp = ReturnSpecifier(s);
            return sp == FormatSpecifier;
        }

        public virtual string[] ArgsRequred { get { return new string[0]; } }
    }
}
