using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Dataspace.Common.Data
{
    [DataContract]
    public sealed class UriQuery:IEnumerable<KeyValuePair<string,string>>
    {
        [DataMember]
        private KeyValuePair<string, string>[] _parameters = new KeyValuePair<string, string>[0];

        [IgnoreDataMember]
        private readonly static  Regex _validate = new Regex(@"(\??(?<pair>[\w]+=[\w]+)(&\k<pair>)*)?");

        public override string ToString()
        {
            if (!_parameters.Any())
                return "";
            return string.Format("?{0}",
                                string.Join("&",
                                            _parameters.Select(k=>string.Format("{0}={1}",k.Key,k.Value))));
        }

        public string this[string parameter]
        {
            get { return _parameters.FirstOrDefault(k => k.Key == parameter).Value; }
            set
            {
                for (int i = 0; i < _parameters.Length; i++)
                    if (_parameters[i].Key == parameter)
                    {
                        _parameters[i] = new KeyValuePair<string, string>(parameter, value);
                        break;
                    }
            }
        }

        public UriQuery()
        {
            
        }

        public UriQuery(IEnumerable<KeyValuePair<string,string>> query ):this()
        {
            _parameters = query.ToArray();
        }


        public UriQuery(UriQuery query)
        {
            _parameters = query._parameters;
        }

        public UriQuery(string query)
        {
            if (!_validate.IsMatch(query))
            {
                Debug.Fail("Неверный запрос");
                throw new InvalidOperationException("Неверный запрос");
            }
            if (!string.IsNullOrEmpty(query))
            _parameters =
                query.TrimStart('?')
                    .Split('&')
                    .Select(k => k.Split('='))
                    .Select(k => new KeyValuePair<string, string>(k[0], k[1]))
                    .ToArray();

        }

        public void Add(string key,string value)
        {
            Array.Resize(ref _parameters, _parameters.Length + 1);
            _parameters[_parameters.Length-1] = new KeyValuePair<string, string>(key,value);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return (_parameters as IEnumerable<KeyValuePair<string,string>>).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }
    }
}
