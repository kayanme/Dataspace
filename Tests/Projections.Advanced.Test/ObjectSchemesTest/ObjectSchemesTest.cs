using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Projections.Advanced.Test.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Projections.Advanced.Test
{
    [TestClass]
    public class ObjectScemes
    {
        string Dataspace = "http://metaspace.org/DataSchema";
        string querySpace = "http://metaspace.org/QuerySchema";


        [TestMethod]
        [TestCategory("Projections")]
        public void OneDataSchemeTest()
        {
            

            var container =
                Utils.MakeContainerAndCache(new Settings(),
                                            new[]
                                                {
                                                    typeof (Model),
                                                    typeof (ModelGetter),
                                                    typeof (ModelPoster),
                                                    typeof (ModelQuerier),
                                                    typeof(Registrator)
                                                });
            var projections = container.GetExportedValue<IProjectionsCollector>();
            var set = projections.GetSchemas();
            var schema = set.Schemas(Dataspace).OfType<XmlSchema>().First();            
            var modelName = new XmlQualifiedName("Model", Dataspace);
            Assert.IsTrue(schema.SchemaTypes.Contains(modelName));
            var type = schema.SchemaTypes[modelName] as XmlSchemaComplexType;
            Assert.IsNotNull(type);           
            var dataAttrs = type.AttributeUses.Values
                .OfType<XmlSchemaAttribute>()
                .Where(k =>string.Equals(k.QualifiedName.Namespace, "", StringComparison.InvariantCultureIgnoreCase))
                .ToArray();
            Assert.AreEqual(2, dataAttrs.Count());
            var idAttr = dataAttrs.First(k=>k.Name == "Id");        
            Assert.AreEqual("", idAttr.QualifiedName.Namespace);
            Assert.AreEqual("Guid",idAttr.AttributeSchemaType.Name,true);
         
        }

        [TestMethod]
        [TestCategory("Projections")]
        public void OneQuerySchemeTest()
        {
            var container =
              Utils.MakeContainerAndCache(new Settings(),
                                              new[]
                                                  {
                                                      typeof (Model), 
                                                      typeof (ModelGetter), 
                                                      typeof (ModelPoster),
                                                      typeof (ModelQuerier),
                                                      typeof(Registrator)
                                                  });

            var projections = container.GetExportedValue<IProjectionsCollector>();
            var set = projections.GetSchemas();
            var schema = set.Schemas(Dataspace).OfType<XmlSchema>().First();            
            var modelName = new XmlQualifiedName("Model", Dataspace);
            var type = schema.SchemaTypes[modelName] as XmlSchemaComplexType;

            var queryAttrs = type.AttributeUses.Values
                    .OfType<XmlSchemaAttribute>()
                    .Where(k =>string.Equals(k.QualifiedName.Namespace, querySpace,StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();
            Assert.AreEqual(3, queryAttrs.Length);
            var attrs = queryAttrs;         
            Assert.IsTrue(attrs.Any(k => string.Equals(k.QualifiedName.Name, "name", StringComparison.InvariantCultureIgnoreCase)));
            Assert.IsTrue(attrs.Any(k => string.Equals(k.QualifiedName.Name, "element", StringComparison.InvariantCultureIgnoreCase)));
            Assert.IsTrue(attrs.Any(k => string.Equals(k.QualifiedName.Name, "isRoot", StringComparison.InvariantCultureIgnoreCase)));
            foreach (var attr in attrs)
            {
                switch(attr.Name)
                {
                    case "name":
                        Assert.AreEqual("string",attr.SchemaTypeName.Name,true);
                        break;
                    case "element":
                        Assert.AreEqual("guid", attr.SchemaTypeName.Name, true);
                        break;
                    case "isRoot":
                        Assert.AreEqual("boolean", attr.SchemaTypeName.Name, true);
                        break;
                }
            }
        }

    }
}
