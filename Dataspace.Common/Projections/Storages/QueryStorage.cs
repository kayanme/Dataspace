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
using Dataspace.Common.Projections.Storages;
using Dataspace.Common.Utility;

namespace Dataspace.Common.Services
{

    [Export,Export(typeof(IInitialize))]
    [PartCreationPolicy(CreationPolicy.Shared)]    
    internal class QueryStorage:IInitialize
    {
        private IndexedCollection<ResourceQuerier.FuncWithSortedArgs> _queries;
        private IndexedCollection<ResourceQuerier.SeriesFuncWithSortedArgs> _seriesQueries;
#pragma warning disable 0649
        [Import(AllowDefault = true)]
        private IResourceQuerierFactory _querierFactory;

        [Import]
        private DefaultFabrica _fabrica;

        [Import]
        private RegistrationStorage _registrationStorage;

        [ImportMany(AllowRecomposition = true)]
        private IEnumerable<ResourceQuerier> _queriers;
              
        [Import]
        private IGenericPool _cachier;
#pragma warning restore 0649

        private readonly StringComparer _parameterComparer = StringComparer.InvariantCultureIgnoreCase;

        public const string QuerySpace = "http://metaspace.org/QuerySchema";

        private void RegisterDefaultQuery(Type type)
        {           
            var querier = _fabrica.DefaultCreator<ResourceQuerier>("обработчик запросов", "CreateQuerier", type, _querierFactory);

            var queries = querier.ReturnQueries();
            Debug.Assert(queries.Count() == 1, "Универсальный обработчик должен содержать один метод с аргументом UriQuery");
            if (queries.Count() != 1)
                throw new InvalidOperationException("Универсальный обработчик должен содержать один метод с аргументом UriQuery");       
            _queries.Add(queries.First());
        }
     
        

        public void SetCachier(IGenericPool cachier)
        {
            _cachier = cachier;
        }

        public void RegisterType(Type type)
        {          
            if (_querierFactory != null)
               RegisterDefaultQuery(type);
        }

      

        [Pure]
        public bool TypeRegistered(Type type)
        {
            return _queries.Any(k=>k.TargetResource == type);
        }   

        private Func<object[], IEnumerable<Guid>> FindFunctionToExecute(
                    Type type,
                    string nmspace,
                    string realQueryNamespace,
                    string[] parameters)
        {
            if (_queries.All(k => k.Namespace != nmspace))
            {
                return null;
            }
            var parNames = parameters.OrderBy(k2 => k2).ToArray();
            var correctOrder =
                parameters.Select((k, ind) => new {k, ind})
                          .OrderBy(k => k.k)
                          .Select(k => k.ind)
                          .ToArray();

            var queryGroup = _queries.Where(k => k.TargetResource == type
                                              && _parameterComparer.Equals(k.Namespace, nmspace)
                                              && k.LengthIndex == parameters.Length);

            if (queryGroup.Any())
            {
                var exactQuery = queryGroup.FirstOrDefault(
                    k => k.Args.Zip(parNames, (a, b) => new { a, b }).All(k2 => _parameterComparer.Equals(k2.a, k2.b)));

                if (exactQuery != null)
                {                    
                    return qr => exactQuery.UnconversedArgsFunction(correctOrder.Select(k=>qr[k]).ToArray());
                }
            }

            //пытаемся найти универсальный запрос в данном неймспейсе
            queryGroup = _queries.Where(k => k.TargetResource == type && _parameterComparer.Equals(k.Namespace, nmspace) && k.LengthIndex == -1);
            if (queryGroup.Any())
            {
                if (string.IsNullOrEmpty(realQueryNamespace))
                {

                    return qr =>
                               {
                                   var query =
                                       new UriQuery(parNames.Zip(correctOrder.Select(k => qr[k]),
                                                                 (a, b) =>
                                                                 new KeyValuePair<string, string>(a, b.ToString())));
                                   return queryGroup.First().UniversalFunction(query);
                               };
                }
                else
                {
                    return qr =>
                               {
                                   var query =
                                       new UriQuery(parNames.Zip(correctOrder.Select(k => qr[k]),
                                                                 (a, b) =>
                                                                 new KeyValuePair<string, string>(a, b.ToString())));
                                   var namespacedQuery = new UriQuery(query) {{"namespace", realQueryNamespace}};
                                   return queryGroup.First().UniversalFunction(namespacedQuery);
                               };
                }
            }
            else
                return null;
        }


        private Func<UriQuery, IEnumerable<Guid>> FindQueryToExecute(
            Type type,
            string nmspace,
            string realQueryNamespace,
            UriQuery query)
        {
            if (_queries.All(k => k.Namespace != nmspace))
            {
                return null;
            }
            var parNames = query.OrderBy(k2 => k2.Key).Select(k => k.Key).ToArray();
          
            var queryGroup = _queries.Where(k => k.TargetResource == type && _parameterComparer.Equals(k.Namespace, nmspace) && k.LengthIndex == query.Count());

            if (queryGroup.Any())
            {
                var exactQuery = queryGroup.FirstOrDefault(
                    k => k.Args.Zip(parNames, (a, b) => new { a, b }).All(k2 => _parameterComparer.Equals(k2.a, k2.b)));

                if (exactQuery != null)
                {
                    return qr =>
                    {
                        var oq = qr.OrderBy(k2 => k2.Key).ToArray();
                        return exactQuery.Function(oq.Select(k => k.Value).ToArray());
                    };
                }
            }

            //пытаемся найти универсальный запрос в данном неймспейсе
            queryGroup =_queries.Where(k => k.TargetResource == type &&_parameterComparer.Equals(k.Namespace,nmspace) && k.LengthIndex == -1).ToArray();
            if (queryGroup.Any())
            {
                var qg = queryGroup.First();
                if (string.IsNullOrEmpty(realQueryNamespace))
                {                    
                    return qr => qg.UniversalFunction(qr);
                }
                else
                {                
                    return qr =>
                    {
                        var namespacedQuery = new UriQuery(qr) { { "namespace", realQueryNamespace } };    
                        return qg.UniversalFunction(namespacedQuery);
                    };
                }

            }
            else
                return null;
        }

        public Func<UriQuery, IEnumerable<Guid>> FindQuery(Type type, string nmspace, UriQuery query)
        {
            Contract.Requires(query != null);
            Contract.Requires(_queries.Any(k=>k.TargetResource == type));
            Contract.Ensures(Contract.Result<Func<UriQuery, IEnumerable<Guid>>>() != null);
            Debug.Assert(query != null);
            if (_queries.All(k => k.TargetResource != type))
                throw new InvalidOperationException("Невозможно найти запрос (не найдено вообще запросов для типа " + type + ").");

        
            if (nmspace != "")
            {
                var res = FindQueryToExecute(type, nmspace, nmspace, query);
                if (res != null)
                    return res;
            }

            var baseRes = FindQueryToExecute(type, "", nmspace, query);
            if (baseRes == null)
                throw new InvalidOperationException("Невозможно найти запрос для типа " + type + "(не найден обработчик). Запрос: " +
                                                    query);

            return baseRes;
        }

        public Func<object[], IEnumerable<Guid>> FindQuery(Type type, string nmspace, string[] parameters)
        {
            Contract.Requires(parameters != null);
            Contract.Requires(_queries.Any(k => k.TargetResource == type));
            Contract.Ensures(Contract.Result<Func<UriQuery, IEnumerable<Guid>>>() != null);
            Debug.Assert(parameters != null);
            if (_queries.All(k => k.TargetResource != type))
                throw new InvalidOperationException("Невозможно найти запрос (не зарегистрирован тип " + type + ").");


            if (nmspace != "")
            {
                var res = FindFunctionToExecute(type, nmspace, nmspace, parameters);
                if (res != null)
                    return res;
            }

            var baseRes = FindFunctionToExecute(type, "", nmspace, parameters);
            if (baseRes == null)
                throw new InvalidOperationException("Невозможно найти запрос для типа " + type + "(не найден обработчик). Запрос: " +
                                                    parameters);

            return baseRes;
        }

        public IEnumerable<ResourceQuerier.FuncWithSortedArgs> FindAppropriateQueries(string nmspc,  string childResource)        
        {
            if ( !_cachier.IsRegistered(childResource))
                return null;
            var resType = _cachier.GetTypeByName(childResource);
            return _queries.Where(k => _parameterComparer.Equals(k.Namespace, nmspc) 
                                    && k.TargetResource == resType);
       

        }

        public IEnumerable<ResourceQuerier.SeriesFuncWithSortedArgs> FindAppropriateSeriaQueries(string nmspc, string parentResource, string childResource)
        {
            if (!_cachier.IsRegistered(childResource))
                return null;

            var resType = _cachier.GetTypeByName(childResource);
            var nameSpacedRes =
                _seriesQueries.Where(
                    k => _parameterComparer.Equals(k.Namespace, nmspc)
                      && k.TargetResource == resType 
                      && _parameterComparer.Equals(k.ResourceName, parentResource));


           
            return nameSpacedRes.ToArray();
            
        }

        internal void Initialize(IEnumerable<ResourceQuerier> queriers)
        {
           
            _queries = new IndexedCollection<ResourceQuerier.FuncWithSortedArgs>(k => k.TargetResource, k => k.Namespace, k => k.LengthIndex);
            _queries.AddRange(queriers.SelectMany(q => q.ReturnQueries()));

            _seriesQueries = new IndexedCollection<ResourceQuerier.SeriesFuncWithSortedArgs>(k => k.TargetResource, k => k.Namespace, k => k.Args.Length, k => k.ResourceName);
            _seriesQueries.AddRange(queriers.SelectMany(q => q.ReturnSeriesQueries()));
        }

        internal XmlSchema GetQuerySchema()
        {
            var schema = new XmlSchema {TargetNamespace = QuerySpace};
            schema.Namespaces.Add("", QuerySpace);
            var allParameters = new StringCollection();
            foreach (var queriesForResourceType in _queries.GroupBy(k => k.TargetResource)
                                                           .Where(k=>_registrationStorage.IsResourceRegistered(k.Key)))
            {
                
                var group = new XmlSchemaAttributeGroup();
                group.Name = _registrationStorage[queriesForResourceType.Key].Replace(" ", "_");
                var parameters = queriesForResourceType.SelectMany(k => k.Args).Distinct().ToArray();
               
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

        public int Order { get { return 2; } }
        public void Initialize()
        {
            Initialize(_queriers);
        }
    }
}
