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
        [Import(AllowDefault = true)]
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

        private Query FindQueryInNamespaceToExecute(
                    Guid resKey,
                    string nmspace,
                    ParameterNames parameters)
        {

            var exactQuery = _baseQueries.FirstOrDefault(k => k.QueryInfo.ResourceKey == resKey
                                                              &&
                                                              _parameterComparer.Equals(k.QueryInfo.Namespace, nmspace)
                                                              && k.QueryInfo.ArgCount == parameters.Count
                                                              && parameters == k.QueryInfo.Arguments);


            if (exactQuery != null)
            {
                return exactQuery;
            }
            return null;

        }

        private Query FindInCache(Guid key,string nmspace,ParameterNames parameters)
        {
            return _cachedQueries.FirstOrDefault(k => k.ResourceKey == key && k.Namespace == nmspace && k.Arguments == parameters);
        }       

        private void AddQueryToCache(Query query)
        {
            _cachedQueries.Add(query);
        }

        private Query BuildOrFindQueryToExecute(Type type, string nmspace, ParameterNames parameters)
        {
            Contract.Requires(parameters != null);
            Contract.Ensures(Contract.Result<FormedQuery>() != null);         
            if (!_registrationStorage.IsResourceRegistered(type))
                throw new InvalidOperationException("Невозможно найти запрос (не зарегистрирован тип " + type + ").");
            var key = _registrationStorage[type].ResourceKey;
            var query = FindInCache(key, nmspace, parameters);
            if (query == null)
            {
               
                query = FindQueryInNamespaceToExecute(key, nmspace, parameters);                                    
                
                if (query == null && nmspace != "")
                {
                    query = FindQueryInNamespaceToExecute(key, "", parameters);
                }

                if (query == null && _querierFactory != null)
                {
                    query =Query.CreateFromFactory(key,_registrationStorage[key].ResourceName, nmspace, parameters,_querierFactory);
                }

                if (query !=null)
                    AddQueryToCache(query);
            }

            if (query ==null)
                throw new InvalidOperationException("Невозможно найти запрос для типа "
                                                   + type + "(не найден обработчик). Запрос: "
                                                   + parameters);
            return query;
        }

        public Func<UriQuery, IEnumerable<Guid>> FindQuery(Type type, string nmspace, UriQuery query)
        {
            Contract.Requires(query != null);            
            Contract.Ensures(Contract.Result<Func<UriQuery, IEnumerable<Guid>>>() != null);
            Debug.Assert(query != null);
            var matchedQuery = BuildOrFindQueryToExecute(type, nmspace, new ParameterNames(query));
            return matchedQuery.UriQuery;
        }

        public FormedQuery FindQuery(Type type, string nmspace, string[] parameters)
        {
            Contract.Requires(parameters != null);
            Contract.Ensures(Contract.Result<FormedQuery>() != null);
            Debug.Assert(parameters != null);
            var matchedQuery = BuildOrFindQueryToExecute(type, nmspace, new ParameterNames(parameters));
            return matchedQuery.GetQueryMethod(parameters);
        }

        public IEnumerable<Query> FindAppropriateQueries(string nmspc,string parentResource,string childResource)        
        {
            if (!_registrationStorage.IsResourceRegistered(childResource))
                return null;
            var resKey = _registrationStorage[childResource].ResourceKey;
            return _baseQueries.Where(k =>k.ResourceKey == resKey)
                               .Where(k=>(string.IsNullOrEmpty(k.QueryInfo.Namespace) || _parameterComparer.Equals(k.QueryInfo.Namespace, nmspc)))
                               .Where(k=>k.Arguments.Contains(parentResource));
       

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
