using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Common.ParsingServices;

namespace Dataspace.Common.ParsingServices
{
    public sealed class UniversalBaseTypeParser
    {

        private ParsingBlock<Guid> _guidParser = new ParsingBlock<Guid>();
        private ParsingBlock<string> _stringParser = new ParsingBlock<string>();
        private ParsingBlock<int> _intParser = new ParsingBlock<int>();
        private ParsingBlock<long> _longParser = new ParsingBlock<long>();
        private ParsingBlock<short> _shortParser = new ParsingBlock<short>();
        private ParsingBlock<bool> _boolParser = new ParsingBlock<bool>();
        private ParsingBlock<double> _doubleParser = new ParsingBlock<double>();
        private ParsingBlock<DateTime> _timeParser = new ParsingBlock<DateTime>();
        private ParsingBlock<Guid?> _nGuidParser = new ParsingBlock<Guid?>();

        
        public Func<string, object> ReturnConversionFunction(Type type)
        {

            Func<Func<string, object>, Func<string, object>> checkWrapper =
                func => s =>
                {
                    try
                    {
                        return func(s);
                    }
                    catch (FormatException ex)
                    {

                        throw new ArgumentException("Значение параметра невозможно преобразовать к необходимому",ex);
                    }
                };

            if (type == typeof(string))
                return checkWrapper(_stringParser.Parse);

            if (type == typeof(Guid))
                return checkWrapper(s=>_guidParser.Parse(s));

            if (type == typeof(int))
                return checkWrapper(s => _intParser.Parse(s));

            if (type == typeof(long))
                return checkWrapper(s => _longParser.Parse(s));

            if (type == typeof(short))
                return checkWrapper(s => _shortParser.Parse(s));

            if (type == typeof(bool))
                return checkWrapper(s => _boolParser.Parse(s));

            if (type == typeof(double))
                return checkWrapper(s => _doubleParser.Parse(s));

            if (type == typeof(DateTime))
                return checkWrapper(s => _timeParser.Parse(s));

                if (type == typeof(Guid?))
                    return checkWrapper(s => _nGuidParser.Parse(s));

            Debug.Fail("Нет преобразования типа для параметра типа " + type.Name);
            throw new InvalidOperationException("Нет преобразования типа для параметра типа " + type.Name);
        }

        public object Converse(Type type,string s)
        {            

            if (type == typeof(string))
                return _stringParser.Parse(s);

            if (type == typeof(Guid))
                return  _guidParser.Parse(s);

            if (type == typeof(int))
                return _intParser.Parse(s);

            if (type == typeof(long))
                return  _longParser.Parse(s);

            if (type == typeof(short))
                return  _shortParser.Parse(s);

            if (type == typeof(bool))
                return  _boolParser.Parse(s);


            if (type == typeof(double))
                return _doubleParser.Parse(s);

            if (type == typeof(DateTime))
                return  _timeParser.Parse(s);

            if (type == typeof(Guid?))
                return  _nGuidParser.Parse(s);

            Debug.Fail("Нет преобразования типа для параметра типа " + type.Name);
            throw new InvalidOperationException("Нет преобразования типа для параметра типа " + type.Name);
        }

        public object Converse(Type type, string s,IDictionary<string,object> args)
        {

            if (type == typeof(string))
                return _stringParser.Parse(s, args);

            if (type == typeof(Guid))
                return _guidParser.Parse(s, args);

            if (type == typeof(int))
                return _intParser.Parse(s, args);

            if (type == typeof(long))
                return _longParser.Parse(s, args);

            if (type == typeof(short))
                return _shortParser.Parse(s, args);

            if (type == typeof(bool))
                return _boolParser.Parse(s, args);


            if (type == typeof(double))
                return _doubleParser.Parse(s, args);

            if (type == typeof(DateTime))
                return _timeParser.Parse(s, args);

            if (type == typeof(Guid?))
                return _nGuidParser.Parse(s);

            Debug.Fail("Нет преобразования типа для параметра типа " + type.Name);
            throw new InvalidOperationException("Нет преобразования типа для параметра типа " + type.Name);
        }
    }
}
