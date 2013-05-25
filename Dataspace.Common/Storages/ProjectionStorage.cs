using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Xml.Schema;
using Dataspace.Common;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Projections.Classes;
using Dataspace.Common.Projections.Storages;
using Dataspace.Common.Services;

namespace Dataspace.Common.Projections
{
    [Export,Export(typeof(IInitialize))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ProjectionStorage:IInitialize
    {
#pragma warning disable 0649

        [Import]
        private QueryStorage _queries;

        [Import]
        private IGenericPool _innerCachier;

        [ImportMany]
        private IEnumerable<PropertiesProvider> _virtualProviders;
#pragma warning restore 0649
       

        private List<ProjectionElement> _globalElements = new List<ProjectionElement>();

        private List<Relation> _relations = new List<Relation>();


        private Dictionary<string, Dictionary<string, PropertiesProvider.PropertyFiller>>
          _projectionResources;

        private IEnumerable<string> ReturnNamespaces(PropertiesProvider prov)
        {
            var attrs = prov.GetType()
                .GetCustomAttributes(typeof(QueryNamespaceAttribute), false)
                .OfType<QueryNamespaceAttribute>()
                .Select(k => k.Namespace)
                .ToArray();
            if (!attrs.Any())
                throw new InvalidOperationException("Описатель ресурсов проекции должен иметь атрибут пространства имен проекции");

            return attrs;
        }
     
        private ProjectionElement CreateElement(XmlSchemaElement element)
        {
            ProjectionElement newElement;
            var nmspc = element.QualifiedName.Namespace;
            var name = element.QualifiedName.Name;
            var schemeType = GetResourceElementType(element);
            Type currentResourceType = null;
            if (schemeType!=null)
            {
               
                newElement = new ResourceProjectionElement
                                 {
                                     Name = name,
                                     Namespace = nmspc,
                                     ResourceType = schemeType,
                                     SchemeType = schemeType
                                 };
                currentResourceType = _innerCachier.GetTypeByName(schemeType);
            }
            else
            {
                newElement = new ProjectionElement
                {
                    Name = name,
                    Namespace = nmspc
                };
            }
            if (_projectionResources.ContainsKey(nmspc) && _projectionResources[nmspc].ContainsKey(name))
                newElement.PropertyFiller = _projectionResources[nmspc][name];
            newElement.FillingInfo = new FillingInfo();
            foreach(var property in (element.ElementSchemaType as XmlSchemaComplexType)
                .AttributeUses
                .Values
                .OfType<XmlSchemaAttribute>()
                .Where(k=>k.QualifiedName.Namespace != QueryStorage.QuerySpace)
                .Select(k => k.QualifiedName.Name))
            {
                if (currentResourceType != null
                && (currentResourceType.GetProperty(property) != null
                 || currentResourceType.GetField(property) != null))
                    newElement.FillingInfo.Add(property,FillingInfo.FillType.Native);
                else
                {
                    newElement.FillingInfo.Add(property, FillingInfo.FillType.ByFiller);
                }
            }

            if (element.ElementSchemaType.Name != null)
            {
                Debug.Assert(FindElement(element) == null);
                _globalElements.Add(newElement);
            }
            return newElement;
        }

        private string GetResourceElementType(XmlSchemaElement element)
        {
            var type = element.ElementSchemaType
                              .Construct(
                                   k => k != null
                                     && !string.Equals(k.QualifiedName.Namespace, 
                                                      RegistrationStorage.Dataspace, 
                                                      StringComparison.InvariantCultureIgnoreCase),
                                   k => k.BaseXmlSchemaType
               ).Last();

            if (type.BaseXmlSchemaType!=null)
                return type.BaseXmlSchemaType.Name;
            return null;
        }

        private ProjectionElement FindElement(XmlSchemaElement element)
        {
            var nmspc = element.QualifiedName.Namespace;
            var name = element.QualifiedName.Name;
            var type = GetResourceElementType(element);
            return _globalElements.SingleOrDefault(k => k.Name == name && k.Namespace == nmspc && k.SchemeType == type);
        }

        private Relation CreateRelation(ProjectionElement parent, ProjectionElement child)
        {
            var newRelation = new Relation { ChildElement = child, ParentElement = parent };
            if (child.SchemeType != null)
            {
                var queries = _queries.FindAppropriateQueries(parent.Namespace,parent.Name, child.SchemeType);             
                newRelation.Queries = queries;                
            }
            else
            {
                newRelation.Queries = null;                
            }
            _relations.Add(newRelation);
            return newRelation;
        }

        private Relation FindRelation(ProjectionElement parent, ProjectionElement child)
        {
            return _relations.SingleOrDefault(k => k.ParentElement == parent && k.ChildElement == child);
        }


        private IEnumerable<XmlSchemaElement> GetAllowedChilds(XmlSchemaComplexType elementType)
        {
            return elementType.ContentType == XmlSchemaContentType.Empty
                ? new XmlSchemaElement[0]
                : elementType
                     .ContentTypeParticle.CastSingle<XmlSchemaSequence>()
                     .Items
                     .OfType<XmlSchemaElement>()
                     .ToArray();
        }


        private ProjectionElement ProcessElement(XmlSchemaElement element,ProjectionElement taggedElement)
        {
            var node = (element.ElementSchemaType.Name != null ? FindElement(element) : taggedElement) 
                    ?? CreateElement(element);
            Debug.Assert(node != null);
            var childElements = GetAllowedChilds(element.ElementSchemaType.CastSingle<XmlSchemaComplexType>());

            foreach (var childElement in childElements)
            {
                var childNode = (childElement.ElementSchemaType.Name != null 
                                ? FindElement(childElement) 
                                : null) 
                             ?? CreateElement(childElement);
                Debug.Assert(childElement != null);
                var relation = (element.ElementSchemaType.Name != null && childElement.ElementSchemaType.Name != null) 
                                    ? FindRelation(node, childNode):null;
                if (relation == null)
                {
                    relation = CreateRelation(node, childNode);
                    Debug.Assert(relation.HasTrivialQuery ||
                        relation.Queries !=null);
                    node.DownRelations.Add(relation);
                    childNode.UpRelations.Add(relation);
                    ProcessElement(childElement, childNode);
                }
            }
            return node;
        }

        private void MarkProviders()
        {
            var customResourcesGroups =
                _virtualProviders.Select(k => new {spaces = ReturnNamespaces(k), fillers = k.CollectPropertiesFillers()})
                    .SelectMany(k => k.spaces.Select(k2 => new {space = k2, k.fillers}))
                    .GroupBy(k => k.space);

            var customResourceGroupsBySpaceAndName =
                customResourcesGroups.Select(k => new
                                                      {
                                                          space = k.Key,
                                                          names = k.SelectMany(k2 => k2.fillers)
                                                      .GroupBy(k2 => k2.Key)
                                                      }).ToArray();

            foreach (var group in customResourceGroupsBySpaceAndName)
                foreach (var name in group.names)
                {
                    if (name.Count() != 1)
                        throw new InvalidOperationException(
                            string.Format("Зарегистрировано больше одного получателя для ресурса {0} проекции {1}",
                                          name.Key, group.space));
                }

            _projectionResources = customResourceGroupsBySpaceAndName.ToDictionary(k => k.space,
                                                                                   k => k.names
                                                                                            .ToDictionary(k2 => k2.Key,
                                                                                                          k2 =>
                                                                                                          k2.Single().
                                                                                                              Value));
        }

        public ProjectionElement FindElement(string name,string nmspc)
        {
            return _globalElements.SingleOrDefault(k => k.Name == name && k.Namespace == nmspc);
        }

        public void ProcessElements(IEnumerable<XmlSchemaElement> elements)
        {
            foreach (var xmlSchemaElement in elements)
            {
                if (FindElement(xmlSchemaElement) == null)
                {
                    var node = ProcessElement(xmlSchemaElement, null);
                    if (FindElement(xmlSchemaElement) == null)
                        _globalElements.Add(node);
                }
            }
        }


        public void Clean()
        {
            _globalElements.Clear();
        }

        public int Order { get { return 13; } }
        public void Initialize()
        {
            MarkProviders();  
        }
    }
}
