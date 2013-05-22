﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Projections.Classes.Plan
{
    public class ParameterNames:IEnumerable<string>
    {
        public IEnumerator<string> GetEnumerator()
        {
            return _names.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private readonly string[] _names;

        private readonly int _hash;


        public override string ToString()
        {
            return string.Join(";", _names);
        }

        public ParameterNames(IEnumerable<string> names)
        {
            _names = names.OrderBy(k => k).Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();
            unchecked
            {
                int sum = 0;
                foreach (string k in _names)
                    sum += k.GetHashCode();
                _hash = sum;
            }
        }

        public ParameterNames():this(new string[0])
        {           
        }

        public static bool operator == (ParameterNames n1,ParameterNames n2)
        {
            if (ReferenceEquals(n1 ,null)|| ReferenceEquals(n2, null))
                Debugger.Break();

            if (n1._hash != n2._hash)
                return false;

            if (n1._names.Length!=n2._names.Length)
                return false;
            return !n1._names.Where((t, i) => t != n2._names[i]).Any();
        }

        public static bool operator !=(ParameterNames n1, ParameterNames n2)
        {
            if (ReferenceEquals(n1, null) || ReferenceEquals(n2, null))
                Debugger.Break();

            if (n1._hash != n2._hash)
                return true;

            if (n1._names.Length != n2._names.Length)
                return true;
            return n1._names.Where((t, i) => t != n2._names[i]).Any();
        }

        public override bool Equals(object obj)
        {
            return this == (obj as ParameterNames);
        }

        public override int GetHashCode()
        {
            return _hash;
        }
    }
}