using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Projections.Classes.Plan;

namespace Dataspace.Common.ServiceResources
{
    using KeyPair = KeyValuePair<Guid, IEnumerable<Guid>>;
    using MultKeys = IEnumerable<KeyValuePair<Guid, IEnumerable<Guid>>>;
    public sealed class Query
    {
        private string[] _orderedArguments;
        public ParameterNames Arguments { get { return QueryInfo.Arguments; } }
        public string Namespace {get { return QueryInfo.Namespace; }}
        public Guid ResourceKey{get { return QueryInfo.ResourceKey; }}
        public int ArgCount{get { return QueryInfo.ArgCount; }}

        private Func<string, object>[] _conversions;
        private Type[] _types;
        private Type _returnType;
        private Func<object[],object> _queryMethod;

        public QueryInfo QueryInfo { get; private set; }

        private int GetArgumentIndex(string name)
        {
            var index = Array.IndexOf(_orderedArguments, _orderedArguments.First(k2 => StringComparer.InvariantCultureIgnoreCase.Equals(k2, name)));
            if (index == -1)
                throw new InvalidOperationException();
            return index;
        }

        internal int[] CreateReorderSeq(string[] parameters)
        {
            if (parameters.Length == 0 && ArgCount == 0)
                return new int[0];

            if (parameters.Zip(_orderedArguments, StringComparer.InvariantCultureIgnoreCase.Equals).All(k => k))
                return Enumerable.Range(0, parameters.Length).ToArray();

            var correctOrder =
                       _orderedArguments
                           .Select(
                               k => Array.IndexOf(parameters, parameters.First(k2 => StringComparer.InvariantCultureIgnoreCase.Equals(k2, k))))
                           .ToArray();
            return correctOrder;
        }

        public Func<string,object> GetConversionForParameter(string name)
        {
            var index = GetArgumentIndex(name);
            return _conversions[index];
        }

        public bool SerialQueryIsPreferred(string parentName)
        {
            var index = GetArgumentIndex(parentName);
            var targetType = _types[index];
            return targetType == typeof(IEnumerable<Guid>);
        }

        public FormedQuery GetQueryMethod(params string[] parametersInOrder)
        {
            var reordering = CreateReorderSeq(parametersInOrder);
            if (_returnType == typeof(MultKeys))
            {
                return args => (_queryMethod(Reorder(args, reordering)) as MultKeys).SelectMany(k => k.Value);
            }
            else if (_returnType == typeof(IEnumerable<Guid>))
            {
                return args =>
                {
                    var res = _queryMethod(Reorder(args, reordering)) as IEnumerable<Guid>;
                    Debug.Assert(res != null);
                    return res;
                };
            }
            else
            {
                throw new ArgumentException();
            }

        }

        public bool CanBeAQueryForParentResource(string parentResourceName)
        {
            return QueryInfo.Arguments.Contains(parentResourceName, StringComparer.InvariantCultureIgnoreCase);
        }

        public QueryForSingleParentResource GetSingleChildResourceQuery(string parentName, params string[] parameters)
        {
            var ar = FormInParameterNames(parentName, parameters);
            
            Func<Guid, object[], object[]> argConv;
            if (!Arguments.Contains(parentName))
            {
                argConv = (key, args) => args.ToArray();
            }
            else if (!SerialQueryIsPreferred(parentName))
            {
                argConv = (key, args) => new object[] { key }.Concat(args).ToArray();
            }
            else
            {
                argConv = (key, args) => new object[] { new[] { key } }.Concat(args).ToArray();
            }

            var method = GetQueryMethod(ar);
            return (id, args) => method(argConv(id, args));
        }

        private string[] FormInParameterNames(string parentName, string[] parameters)
        {
            String[] ar;
            if (!Arguments.Contains(parentName))
            {
                ar = parameters;
            }
            else
            {
                ar = new string[parameters.Length + 1];
                ar[0] = parentName;
                Array.Copy(parameters, 0, ar, 1, parameters.Length);
            }
            return ar;
        }

        public QueryForMultipleParentResource GetMultipleChildResourceQuery(string parentName, params string[] parameters)
        {
            var ar = FormInParameterNames(parentName, parameters);

            QueryForMultipleParentResource methConv;
            if (_returnType == typeof(IEnumerable<Guid>))
            {
                var method = GetQueryMethod(ar);
                if (!Arguments.Contains(parentName))
                {
                    methConv = (keys, args) =>
                             keys.Select(key => new KeyValuePair<Guid, IEnumerable<Guid>>(key, method(args).ToArray()));
                }
                else if (!SerialQueryIsPreferred(parentName))
                {
                    methConv = (keys, args) =>
                               keys.Select(key => new KeyValuePair<Guid, IEnumerable<Guid>>(key, method(new object[] { key }.Concat(args).ToArray())));
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            else if (_returnType == typeof(MultKeys))
            {
                var reordering = CreateReorderSeq(ar);

                if (!Arguments.Contains(parentName) || !SerialQueryIsPreferred(parentName))
                {
                    throw new ArgumentException();
                }
                else
                {
                    methConv = (keys, args) =>
                                  _queryMethod(Reorder(new object[] { keys }.Concat(args).ToArray(), reordering)) as MultKeys;
                }
            }
            else
            {
                throw new ArgumentException();
            }
            return methConv;
        }

        private static object[] Reorder(IEnumerable<object> args,int[] order)
        {
            var r = args.ToArray();
            return order.Select(k => r[k]).ToArray();
        }

        public IEnumerable<Guid> UriQuery(UriQuery query)
        {
            var args = from pair in query
                       let conv = GetConversionForParameter(pair.Key)
                       let argValue = pair.Value
                       select conv(argValue);
            var method = GetQueryMethod(query.Select(k => k.Key).ToArray());
            return method(args.ToArray());
        }

        internal static Query CreateTestStubQuery(string nmspc = null,Func<object[],object> func = null,params string[] pars)
        {
            var query = new Query(new QueryInfo(Guid.NewGuid(), new ParameterNames(pars), nmspc),
                                  Enumerable.Repeat(typeof(string), pars.Count()).ToArray(),
                                  typeof(IEnumerable<Guid>))
            {
                _orderedArguments = pars.ToArray(),
                _queryMethod = func?? (k => new[] { Guid.Empty }),
            };
            return query;
            
        }

        internal static Query CreateTestStubMultipleQuery(string resource,params string[] pars)
        {
            var query = new Query(new QueryInfo(Guid.NewGuid(), new ParameterNames(new[]{resource}.Concat(pars)), ""),
                                  new[]{ typeof(IEnumerable<Guid>)}.Concat(Enumerable.Repeat(typeof(string), pars.Count())).ToArray(),
                                  typeof(MultKeys))
            {
                _orderedArguments = new[]{resource}.Concat(pars).ToArray(),
                _queryMethod = k => new KeyPair(Guid.Empty,new[]{Guid.Empty })
            };
            return query;

        }

        internal static Query CreateFromFactory(Guid key,string resName, string nmspace, ParameterNames parameters,IResourceQuerierFactory querierFactory)
        {
            var query = new Query(new QueryInfo(key, new ParameterNames(parameters), nmspace),
                                  Enumerable.Repeat(typeof(string),parameters.Count()).ToArray(),
                                  typeof(IEnumerable<Guid>))
            {
                _orderedArguments = parameters.ToArray(),
                _queryMethod = k=> querierFactory.CreateQuerier(resName, nmspace,
                                                  parameters.ToArray())(k),              
            };
            return query;
        }

        public static Query CreateFromMethod(Guid key,string nmspace,object owner,MethodInfo method)
        {
            Debug.Assert(method !=null);
            var query = new Query(
                new QueryInfo(key, new ParameterNames(method.GetParameters().Select(k => k.Name)), nmspace),
                method.GetParameters().Select(k => k.ParameterType).ToArray(),
                method.ReturnType
                )
                            {
                                _queryMethod = args=>method.Invoke(owner,args),
                                _orderedArguments = method.GetParameters().Select(k => k.Name).ToArray()
                            };
            return query;
        }

        internal static Query CreateFromQueryWithArgReorder(Query query,int[] order)
        {
          var newQuery =  new Query(
                  query.QueryInfo,
                  order.Select(k=>query._types[k]).ToArray(),
                  query._returnType)
            {                
                _queryMethod =
                    qr => query._queryMethod(order.Select(k => qr[k]).ToArray()),
                   _orderedArguments = order.Select(k => query._orderedArguments[k]).ToArray(),   
            };
          return newQuery;
        }

        private Query(QueryInfo header,Type[] argTypes,Type returnType)
        {
            QueryInfo = header;
            _types = argTypes.ToArray();
            _conversions = _types.Select(TypeDescriptor.GetConverter)
                                .Select(k =>new Func<string, object>(k.ConvertFromString))
                                .ToArray();
            _returnType = returnType;
        }
    }
}
