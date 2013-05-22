using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Dataspace.Common;
using Dataspace.Common.Attributes;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.ParsingServices;
using Dataspace.Common.Services;

namespace Dataspace.Common.ClassesForImplementation
{
    [ContractClass(typeof(ResourceQuerierContracts))]
    public abstract class ResourceQuerier
    {
       
#pragma warning disable 0649

        [Import]
        protected ITypedPool TypedPool { get; private set; }

        
#pragma warning restore 0649

        protected StringComparer ParameterComparer = StringComparer.InvariantCultureIgnoreCase;

        protected EqualityComparer<string> S = EqualityComparer<string>.Default;

        internal abstract IEnumerable<FuncWithSortedArgs> ReturnQueries();

        internal abstract IEnumerable<SeriesFuncWithSortedArgs> ReturnSeriesQueries();

        internal sealed class FuncWithSortedArgs : BaseFuncWithSortedArgs
        {

            public  override int LengthIndex
            {
                get { return UniversalFunction != null ? -1 : Args.Length; }
            }

            public readonly Func<string[], IEnumerable<Guid>> Function;
            public readonly Func<UriQuery, IEnumerable<Guid>> UniversalFunction;
            public readonly Func<object[], IEnumerable<Guid>> UnconversedArgsFunction;
            

            public FuncWithSortedArgs(string[] args, Func<string[], IEnumerable<Guid>> func,Func<object[], IEnumerable<Guid>> unFunc, Type targetResource, Func<string, object>[] conversions)
                :base(args,conversions)
            {
                Debug.Assert(args.Length == conversions.Length);
                Function = func;
                UnconversedArgsFunction = unFunc;
                TargetResource = targetResource;
            }

            public FuncWithSortedArgs(string[] args, Func<UriQuery, IEnumerable<Guid>> func, Type targetResource)
                : base(args, Enumerable.Repeat(new Func<string,object>(k=>k),args.Length).ToArray())
            {              
                UniversalFunction = func;
                TargetResource = targetResource;
            }
            
        }

        internal abstract class BaseFuncWithSortedArgs
        {
            public readonly string[] Args;
            public string Namespace;
            public Type TargetResource;
            public abstract int LengthIndex { get; }

            public readonly Dictionary<string, Func<string, object>> Conversions; 

            protected BaseFuncWithSortedArgs(string[] args,Func<string,object>[] conversions)
            {
               
              
                Args = args;
                Conversions = new Dictionary<string, Func<string, object>>(args.Zip(conversions,(k,v)=>new {k,v}).ToDictionary(k=>k.k,k=>k.v),StringComparer.InvariantCultureIgnoreCase);
            }
        }

        internal sealed class SeriesFuncWithSortedArgs : BaseFuncWithSortedArgs
        {

            public delegate IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>>
                TargetFunction(IEnumerable<Guid> a, string[] b);

            public delegate IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>>
                TargetUnconversedFunction(IEnumerable<Guid> a, object[] b);

            public TargetFunction Function;
            public TargetUnconversedFunction UnconversedFunction;
            public string ResourceName;

            public  override int LengthIndex
            {
                get { return Args.Length + 1; }
            }

            public SeriesFuncWithSortedArgs(string resName, string[] args, 
                                            TargetFunction func,TargetUnconversedFunction unconversedFunction, 
                                            Type targetResource,
                                            Func<string, object>[] conversions)
                :base(args,conversions)
            {
                Debug.Assert(args.Length == conversions.Length);
                Function = func;
                UnconversedFunction = unconversedFunction;
                ResourceName = resName;
                TargetResource = targetResource;
            }
        }
    }

    public abstract class ResourceQuerier<T> : ResourceQuerier
    {

        private UniversalBaseTypeParser _parser = new UniversalBaseTypeParser();

        #region Imports

#pragma warning disable 0649
        [Import(AllowRecomposition = false, RequiredCreationPolicy = CreationPolicy.Shared)]
        private SettingsHolder _settingsHolder;

        [Import]
        private AppConfigProvider _appConfigProvider;

#pragma warning restore 0649

        #endregion

        private Func<string, object> ReturnConversionFunction(Type type)
        {
            return _parser.ReturnConversionFunction(type);
        }

        private FuncWithSortedArgs MakeUniversalFunction(MethodInfo queryFunc)
        {
            return new FuncWithSortedArgs(new string[0],
                                             k =>
                                             queryFunc.Invoke(this, new object[] { k }) as IEnumerable<Guid>, typeof(T)); 
        }

        private FuncWithSortedArgs Wrapper(MethodInfo queryFunc)
        {
            Debug.Assert(queryFunc.ReturnType == typeof (IEnumerable<Guid>));
            var parameters = queryFunc.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof (UriQuery))
            {
                return MakeUniversalFunction(queryFunc);
            }

            var argsInDesiredOrder = parameters.Select((k, ind) => new {ind, k.Name})
                .OrderBy(k => k.Name, ParameterComparer);
            var argsNamesInDesiredOrder = argsInDesiredOrder
                .Select(k => k.Name)
                .ToArray();
            var argsIndexesInDesiredOrder = argsInDesiredOrder
                .Select(k => k.ind)
                .ToArray();
            var parCount = parameters.Length;
            Func<string, object>[] conversions;
            try
            {
                conversions = parameters.Select(k => ReturnConversionFunction(k.ParameterType)).ToArray();
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException("Ошибка в запросе " + queryFunc.Name, ex);
            }
           
            Func<object[], IEnumerable<Guid>> unFunc =
                  (args) =>
                    {
                        var parametersInCorrectOrder =
                            parCount.Times().Select(i => args[argsIndexesInDesiredOrder[i]]).ToArray();
                     
                        object result;
                        try
                        {
                            result = queryFunc.Invoke(this, parametersInCorrectOrder);
                        }
                        catch (TargetInvocationException ex)
                            //если внутри запроса ошибка, надо вернуть внутреннее исключение
                        {
                            throw ex.InnerException;
                        }

                        if (result == null)
                            throw new InvalidOperationException("Запрос не может возвращать null (для отсутствия результатов выборки используй DefaultValue). Тип: "
                                                                + typeof (T)
                                                                + ", класс: " + GetType()
                                                                + ", метод:" + queryFunc.Name);
                        Debug.Assert(result is IEnumerable<Guid>);
                        return result as IEnumerable<Guid>;
                    };


            var convInCorrectOrder = parCount.Times().Select(i => conversions[argsIndexesInDesiredOrder[i]]).ToArray();
            Func<string[], IEnumerable<Guid>> func =
                (args) =>
                    {                       
                        var convArgs = convInCorrectOrder.Zip(args, (a, b) => a(b)).ToArray();
                        return unFunc(convArgs);
                    };
            return new FuncWithSortedArgs(argsNamesInDesiredOrder, func, 
                                          unFunc,typeof (T),
                                         convInCorrectOrder);
        }

        private SeriesFuncWithSortedArgs SeriesWrapper(MethodInfo queryFunc)
        {
            Debug.Assert(queryFunc.ReturnType == typeof (IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>>));
            var parameters = queryFunc.GetParameters();
            Debug.Assert(parameters.Length > 0);
            Debug.Assert(parameters[0].ParameterType == typeof (IEnumerable<Guid>));
            var resDescr = Attribute.GetCustomAttribute(parameters[0], typeof (ResourceAttribute)) as ResourceAttribute;
            Debug.Assert(resDescr != null);

            var resourceType = resDescr.Name;

            var argsInDesiredOrder = parameters.Skip(1)
                .Select((k, ind) => new {ind, k.Name})
                .OrderBy(k => k.Name, ParameterComparer);
            var argsNamesInDesiredOrder = argsInDesiredOrder
                .Select(k => k.Name)
                .ToArray();
            var argsIndexesInDesiredOrder = argsInDesiredOrder
                .Select(k => k.ind)
                .ToArray();
            var parCount = parameters.Length - 1;
            var conversions = parameters.Skip(1).Select(k => ReturnConversionFunction(k.ParameterType)).ToArray();
            SeriesFuncWithSortedArgs.TargetUnconversedFunction unFunc =
                (list, args) =>
                    {
                        var parametersInCorrectOrder =
                            parCount.Times().Select(i => args[argsIndexesInDesiredOrder[i]]).ToArray();
                     
                        object result;
                        try
                        {
                            result = queryFunc.Invoke(this,new[] {list}.Concat(parametersInCorrectOrder).ToArray());
                        }
                        catch (TargetInvocationException ex)
                            //если внутри запроса ошибка, надо вернуть внутреннее исключение
                        {
                            throw ex.InnerException;
                        }

                        if (result == null)
                            throw new InvalidOperationException("Запрос не может возвращать null (для отсутствия результатов выборки используй SeriesDefaultValue). Тип: "
                                                                + typeof (T)
                                                                + ", класс: " + this.GetType()
                                                                + ", метод:" + queryFunc.Name);
                        Debug.Assert(result is IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>>);
                        return result as IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>>;
                    };
            var convInCorrectOrder = parCount.Times().Select(i => conversions[argsIndexesInDesiredOrder[i]]).ToArray();
            SeriesFuncWithSortedArgs.TargetFunction func =
               (list,args) =>
               {
                   var convArgs = convInCorrectOrder.Zip(args, (a, b) => a(b)).ToArray();                 
                   return unFunc(list,convArgs);
               };
            return new SeriesFuncWithSortedArgs(resourceType, argsNamesInDesiredOrder, func, unFunc, typeof(T), convInCorrectOrder);
        }

        private enum QueryType
        {
            SingleQuery,
            MultipleQuery
        };

        private QueryType TestFunction(MethodInfo method)
        {
            if (method.ReturnType == typeof (IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>>))
            {
                var seria = method.GetParameters()[0];
                if (seria.ParameterType == typeof (IEnumerable<Guid>) &&
                    Attribute.GetCustomAttribute(seria, typeof (ResourceAttribute)) != null)
                    return QueryType.MultipleQuery;
                else
                    throw new InvalidOperationException(
                        string.Format("Первым аргументом в запросе {0} класса {1} должен быть IEnumerable<Guid>",
                                      method.Name, method.DeclaringType.Name));
            }
            else if (method.ReturnType == typeof (IEnumerable<Guid>))
            {
                return QueryType.SingleQuery;
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Результатом запроса {0} класса {1} должен быть IEnumerable<Guid> или KeyValuePair<Guid,IEnumerable<Guid>>",
                        method.Name, method.DeclaringType.Name));
            }
        }

        private IEnumerable<MethodInfo> SelectApplicableMethods()
        {
            if (!_settingsHolder.Settings.ActivationSwitchMatch(GetType().GetCustomAttributes(typeof (ActivationSwitchAttribute), true)
                                                             .OfType<ActivationSwitchAttribute>().ToArray(),_appConfigProvider))
                return new MethodInfo[0];

            var allMethods = GetType().GetMethods()
                                      .Where(k =>Attribute.IsDefined(k,(typeof (IsQueryAttribute))));
            var targetMethods =
               allMethods.Where(
                k => _settingsHolder.Settings
                                    .ActivationSwitchMatch(k.GetCustomAttributes(typeof (ActivationSwitchAttribute), true)
                                                                     .OfType<ActivationSwitchAttribute>(),
                                                           _settingsHolder.Provider));

            return targetMethods;
        }

        internal override sealed IEnumerable<FuncWithSortedArgs> ReturnQueries()
        {
            var methods =
              SelectApplicableMethods()
                    .Where(k => TestFunction(k) == QueryType.SingleQuery);

            Debug.Assert(methods.All(m => m.ReturnType == typeof (IEnumerable<Guid>)),
                         "Некорректный результат запроса (должен быть список ключей)");


            var spaces = from meth in methods
                         let spc = meth.GetCustomAttributes(typeof (QueryNamespaceAttribute), false)
                             .Cast<QueryNamespaceAttribute>()
                         select spc.Any()
                                    ? spc.Select(k => k.Namespace).ToArray()
                                    : new[] {""};

            var pairs = methods.Zip(spaces, (meth, space) => new {meth, space})
                .SelectMany(k => k.space.Select(k2 =>
                                                    {
                                                        var func = Wrapper(k.meth);
                                                        func.Namespace = k2;
                                                        return func;
                                                    }));


            return pairs;
        }

        internal override sealed IEnumerable<SeriesFuncWithSortedArgs> ReturnSeriesQueries()
        {
            var methods =
                SelectApplicableMethods()
                    .Where(k => TestFunction(k) == QueryType.MultipleQuery);

            var spaces = from meth in methods
                         let spc = meth.GetCustomAttributes(typeof (QueryNamespaceAttribute), false)
                             .Cast<QueryNamespaceAttribute>()
                         select spc.Any()
                                    ? spc.Select(k => k.Namespace).ToArray()
                                    : new[] {""};

            var pairs = methods.Zip(spaces, (meth, space) => new {meth, space})
                .SelectMany(k => k.space.Select(k2 =>
                                                    {
                                                        var func = SeriesWrapper(k.meth);
                                                        func.Namespace = k2;
                                                        return func;
                                                    }));
            return pairs;
        }

        protected readonly IEnumerable<Guid> DefaultValue = new Guid[0];

        protected readonly IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>> SeriesDefaultValue =
            new KeyValuePair<Guid, IEnumerable<Guid>>[0];
    }

    [ContractClassFor(typeof (ResourceQuerier))]
    internal abstract class ResourceQuerierContracts : ResourceQuerier
    {
        internal override IEnumerable<FuncWithSortedArgs> ReturnQueries()
        {
            Contract.Ensures(Contract.Result<IEnumerable<FuncWithSortedArgs>>() != null);
            return null;
        }

        internal override IEnumerable<SeriesFuncWithSortedArgs> ReturnSeriesQueries()
        {
            Contract.Ensures(Contract.Result<IEnumerable<SeriesFuncWithSortedArgs>>() != null);
            return null;
        }
    }
}
