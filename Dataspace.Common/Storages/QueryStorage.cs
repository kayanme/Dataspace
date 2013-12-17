using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.Projections.Storages;
using Dataspace.Common.ServiceResources;
using Dataspace.Common.Utility;

namespace Dataspace.Common.Services
{

    [Export,Export(typeof(IInitialize))]
    [PartCreationPolicy(CreationPolicy.Shared)]    
    internal class QueryStorage:IInitialize
    {
        private IndexedCollection<Query> _baseQueries;
        private IndexedCollection<Query> _cachedQueries;
     
#pragma warning disable 0649
        [Import(AllowDefault = true,AllowRecomposition = true)]
        private IResourceQuerierFactory _querierFactory;

        [Import]
        private DefaultFabrica _fabrica;

        [Import]
        private RegistrationStorage _registrationStorage;

        [ImportMany(AllowRecomposition = true)]
        private IEnumerable<ResourceQuerier> _queriers;
              
      
#pragma warning restore 0649

        private readonly StringComparer _parameterComparer = StringComparer.InvariantCultureIgnoreCase;

        public const string QuerySpace = "http://metaspace.org/QuerySchema";

        private IEnumerable<Query> FindQueriesInNamespaceToExecute(
                    Guid resKey,
                    string nmspace,
                    ParameterNames parameters)
        {

            var exactQuery = _baseQueries.Where(k => k.QueryInfo.ResourceKey == resKey
                                                     &&
                                                     _parameterComparer.Equals(k.QueryInfo.Namespace, nmspace)
                                                     && k.QueryInfo.ArgCount == parameters.Count
                                                     && parameters == k.QueryInfo.Arguments).ToArray();



            return exactQuery;


        }

        private IEnumerable<Query> FindInCache(Guid key,string nmspace,ParameterNames parameters)
        {
            lock (_cachedQueries)
               return _cachedQueries.Where(k => k.ResourceKey == key && k.Namespace == nmspace && k.Arguments == parameters);
        }       

        private void AddQueryToCache(IEnumerable<Query> queries)
        {
            lock (_cachedQueries)
                _cachedQueries.AddRange(queries);
        }

        private IEnumerable<Query> BuildOrFindQueryToExecute(Type type, string nmspace, ParameterNames parameters)
        {
            Contract.Requires(parameters != null);
            Contract.Ensures(Contract.Result<FormedQuery>() != null);         
            if (!_registrationStorage.IsResourceRegistered(type))
                throw new InvalidOperationException("Невозможно найти запрос (не зарегистрирован тип " + type + ").");
            var key = _registrationStorage[type].ResourceKey;
            var queries = FindInCache(key, nmspace, parameters);
            if (!queries.Any())
            {

                queries = FindQueriesInNamespaceToExecute(key, nmspace, parameters);

                if (!queries.Any() && nmspace != "")
                {
                    queries = FindQueriesInNamespaceToExecute(key, "", parameters);
                }

                if (!queries.Any() && _querierFactory != null)
                {
                    queries = new[] { Query.CreateFromFactory(key, _registrationStorage[key].ResourceName, nmspace, parameters, _querierFactory) };
                }

                if (queries.Any())
                    AddQueryToCache(queries);
            }

            if (!queries.Any())
                throw new InvalidOperationException("Невозможно найти запрос для типа "
                                                   + type + "(не найден обработчик). Запрос: "
                                                   + parameters);
            return queries;
        }

        public Func<UriQuery, IEnumerable<Guid>> FindQuery(Type type, string nmspace, UriQuery query)
        {
            Contract.Requires(query != null);            
            Contract.Ensures(Contract.Result<Func<UriQuery, IEnumerable<Guid>>>() != null);
            Debug.Assert(query != null);
            var matchedQueries = BuildOrFindQueryToExecute(type, nmspace, new ParameterNames(query));
            return matchedQueries.First().UriQuery;
        }

        public FormedQuery FindQuery(Type type, string nmspace, string[] parameters,Type[] paramTypes)
        {
            Contract.Requires(parameters != null);
            Contract.Ensures(Contract.Result<FormedQuery>() != null);
            Debug.Assert(parameters != null);
            var matchedQueries = BuildOrFindQueryToExecute(type, nmspace, new ParameterNames(parameters));
            Debug.Assert(matchedQueries.Any(), "matchedQuery.Any()");
           
            var query = matchedQueries.FirstOrDefault(k => k.TypesAreCompletlyMatch(parameters, paramTypes))
                 ?? matchedQueries.FirstOrDefault(k => k.CanMakeAQueryForArgumentTypes(parameters, paramTypes));
            if (query==null)
                throw new ArgumentException("A query with given parameters was found, but parameters types mismatch");
            return query.GetQueryMethod(parameters,paramTypes);
        }



        public QueryForMultipleParentResource FindQueryWithGrouping(string resource, Type type, string nmspace, string[] parameters, Type[] paramTypes)
        {
            Contract.Requires(parameters != null);
            Contract.Ensures(Contract.Result<FormedQuery>() != null);
            Debug.Assert(parameters != null);
            var matchedQueries = BuildOrFindQueryToExecute(type, nmspace, new ParameterNames(parameters));
            Debug.Assert(matchedQueries.Any(), "matchedQuery.Any()");
            var query = matchedQueries.FirstOrDefault(k => k.SerialQueryIsPreferred(resource))
                     ?? matchedQueries.First();
            return query.GetMultipleChildResourceQuery(resource, parameters.Except(new[] {resource}).ToArray());
        }

        public IEnumerable<Query> FindAppropriateQueries(string nmspc,string parentResource,string childResource)        
        {
            if (!_registrationStorage.IsResourceRegistered(childResource))
                return null;
            var resKey = _registrationStorage[childResource].ResourceKey;
            return _baseQueries.Where(k =>k.ResourceKey == resKey)
                               .Where(k=>(string.IsNullOrEmpty(k.QueryInfo.Namespace) || _parameterComparer.Equals(k.QueryInfo.Namespace, nmspc)));
       

        }
              
       

        internal void Initialize(IEnumerable<ResourceQuerier> queriers)
        {
            _cachedQueries = new IndexedCollection<Query>(k => k.ResourceKey, k => k.Namespace, k => k.ArgCount, k=>k.Arguments);
            _baseQueries = new IndexedCollection<Query>(k => k.ResourceKey, k => k.Namespace, k => k.ArgCount, k => k.Arguments);
            _baseQueries.AddRange(queriers.SelectMany(q => q.ReturnQueries()));
        }

        internal XmlSchema GetQuerySchema()
        {
            var schema = new XmlSchema {TargetNamespace = QuerySpace};
            schema.Namespaces.Add("", QuerySpace);
            var allParameters = new StringCollection();            
            foreach (var queriesForResourceType in _baseQueries.GroupBy(k => k.ResourceKey)
                                                           .Where(k=>_registrationStorage.IsResourceRegistered(k.Key)))
            {
                
                var group = new XmlSchemaAttributeGroup();
                group.Name = _registrationStorage[queriesForResourceType.Key].ResourceName.Replace(" ", "_");
                var parameters = queriesForResourceType.SelectMany(k => k.Arguments)
                                                       .Distinct(_parameterComparer)
                                                       .ToArray();
               
                allParameters.AddRange(parameters);
                foreach (var attr in parameters.Select(k => new XmlSchemaAttribute { RefName = new XmlQualifiedName(k, QuerySpace) }))
                {
                    group.Attributes.Add(attr);
                }
                schema.Items.Add(group);
            }
           foreach(var attr in allParameters.OfType<string>().Distinct().Select(
                k =>
                new XmlSchemaAttribute
                    {
                        Name = k,                        
                        SchemaType = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String)
                    }))
           {
               schema.Items.Add(attr);
           }
            
            return schema;
        }

        public int Order { get { return 4; } }
        public void Initialize()
        {
            Initialize(_queriers);
        }
    }
}
