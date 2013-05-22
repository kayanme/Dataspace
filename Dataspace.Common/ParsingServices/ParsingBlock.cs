using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Common.ParsingServices
{
    internal sealed class ParsingBlock
    {
        internal static IParserResolver ParserGetter { get; private set; }

        private class MefResolver :IParserResolver
        {
            private CompositionContainer _container;
            public MefResolver(CompositionContainer container)
            {
                Contract.Requires(container != null);
                _container = container;
            }

            public IEnumerable<Parser<TK>> Resolve<TK>()
            {
                return _container.GetExportedValues<Parser<TK>>();
            }
        }

        public static void SetMefresolver(CompositionContainer container)
        {
            Contract.Requires(container!=null);
            Contract.Ensures(ParserGetter != null);
            SetResolver(new MefResolver(container));
        }

        public static void SetResolver(IParserResolver resolver)
        {
            ParserGetter = resolver;
        }

         public interface IParserResolver
        {
            IEnumerable<Parser<TK>> Resolve<TK>();

        }

        public delegate IEnumerable<Parser<TK>> ParserResolver<TK>();
    }

    public sealed class ParsingBlock<T>
    {
                    
        private readonly Lazy<IEnumerable<Parser<T>>> _parsers;

        public ParsingBlock() : this(
            ()=> {
                    
                         if (ParsingBlock.ParserGetter == null)
                             throw new InvalidOperationException("Невозможно создать блок: кэш либо не инициализирован, либо требуется явное задание получателя парсеров (если используется не MEF) ");
                       return  ParsingBlock.ParserGetter.Resolve<T>();
                    
                
            })
        {
            
        }

        public ParsingBlock(Func<IEnumerable<Parser<T>>> parsers)
        {          
            Contract.Assume(parsers!=null);  
            _parsers = new Lazy<IEnumerable<Parser<T>>>(parsers);
        }

        public T Parse(string s)
        {
            var pars = _parsers.Value.FirstOrDefault(k => k.IsAppropriateForParsing(s,new string[0]));
            if (pars == null)
                throw new FormatException("Bad string for parsing");
            return pars.Parse(s);
        }

        public T Parse(string s,IDictionary<string,object> args)
        {
            var pars = _parsers.Value.Where(k => k.IsAppropriateForParsing(s,args.Select(k2=>k2.Key).ToArray())).ToArray();
            if (!pars.Any())
                throw new FormatException("Bad string for parsing");
            foreach (var parser in pars)
            {
                try
                {
                 //   var requredArgs = parser.ArgsRequred.Select(k => args[k]).ToArray();
                    return parser.Parse(s, args.Select(k=>k.Value).ToArray());
                }
                catch (FormatException)
                {                                     
                }
                catch (InvalidOperationException)
                {
                }
                catch(KeyNotFoundException)
                {                    
                }
            }
            throw new ArgumentException("s");
        }

        public bool CanBeParsed(string source,string[] args = null)
        {
             var pars = _parsers.Value.Where(k => k.IsAppropriateForParsing(source,args??new string[0]) 
                                               && k.Validate(source)).ToArray();
             return pars.Any();
        }
    }
}
