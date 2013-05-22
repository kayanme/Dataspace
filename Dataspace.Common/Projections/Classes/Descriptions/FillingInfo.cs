using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Projections.Classes
{
    internal sealed class FillingInfo : IEnumerable<KeyValuePair<string, FillingInfo.FillType>>
    {
        public enum FillType
        {
            Native,
            ByFiller
        };


        public string[] PropertyNames { get { return _fillingInfo.Keys.ToArray(); } }

        private readonly Dictionary<string, FillType> _fillingInfo = new Dictionary<string, FillType>();

        public FillType GetFillType(string name)
        {
            return _fillingInfo[name];
        }

        public void Add(string key,FillType value)
        {
            _fillingInfo.Add(key,value);
        }

        public IEnumerator<KeyValuePair<string, FillType>> GetEnumerator()
        {
            return _fillingInfo.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
