using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using Dataspace.Common.ServiceResources;

namespace Dataspace.Common.Projections.Storages.SchemaBuilders
{
    internal sealed class DataSchemaBuilder
    {
        private XmlSchemaSimpleType CreateGuidType()
        {
            var type = new XmlSchemaSimpleType { Name = "Guid" };

            var restriction = new XmlSchemaSimpleTypeRestriction { BaseType = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String) };
            restriction.Facets.Add(new XmlSchemaPatternFacet
            {
                Value =
                    @"[{(]?[0-9A-Fa-f]{8}\-?[0-9A-Fa-f]{4}\-?[0-9A-Fa-f]{4}\-?[0-9A-Fa-f]{4}\-?[0-9A-Fa-f]{12}[})]?|([!$])(\(var|\(loc|\(wix)\.[_A-Za-z][0-9A-Za-z_.]*\)"
            });
            type.Content = restriction;
            return type;
        }

        private XmlNode[] TextToNode(string text)
        {
            var doc = new XmlDocument();
            return new XmlNode[] { doc.CreateTextNode(text) };
        }

        public XmlSchema GetDataSchemas(XmlSchema querySchema,IEnumerable<Registration> registrations)
        {
            var schema = new XmlSchema { TargetNamespace = RegistrationStorage.Dataspace };
            schema.Namespaces.Add("", RegistrationStorage.Dataspace);
            var guid = CreateGuidType();
            schema.Items.Add(guid);
            schema.Includes.Add(new XmlSchemaImport { Schema = querySchema, Namespace = querySchema.TargetNamespace });
            foreach (var registration in registrations)
            {
                var etype = new XmlSchemaComplexType
                {
                    Name = registration.ResourceName,
                    Annotation = new XmlSchemaAnnotation()
                };
                var doc = new XmlSchemaDocumentation { Markup = TextToNode(registration.ResourceType.FullName) };
                etype.Annotation.Items.Add(doc);
                etype.Block = XmlSchemaDerivationMethod.Extension;
                var idAttr = new XmlSchemaAttribute
                {
                    Name = "Id",
                    SchemaTypeName = new XmlQualifiedName(guid.Name, RegistrationStorage.Dataspace),
                    Use = XmlSchemaUse.Required
                };
                etype.Attributes.Add(idAttr);
                var noChildrenAttr = new XmlSchemaAttribute
                {
                    Name = RegistrationStorage.DefinitlyNoChildren,
                    SchemaType = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Boolean),
                    Use = XmlSchemaUse.Optional
                };
                etype.Attributes.Add(noChildrenAttr);
                if (querySchema.Items.OfType<XmlSchemaAttributeGroup>().Any(k => k.Name == etype.Name))
                {

                    var group = new XmlSchemaAttributeGroupRef();
                    group.RefName = new XmlQualifiedName(etype.Name.Replace(" ", "_"), querySchema.TargetNamespace);
                    etype.Attributes.Add(group);

                }
                schema.Items.Add(etype);
            }

            return schema;
        }
    }
}
