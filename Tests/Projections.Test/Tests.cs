using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Utility;
using Hierarchies.Test.Data;
using Projections.Test.Data;
using Server.Modules.HierarchiesModule;
using Testhelper;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Attribute = Projections.Test.Data.Attribute;


namespace Hierarchies.Test.Data
{
    [TestClass]
    public class Tests
    {
        private CompositionContainer container;
        [TestInitialize]
        public void Preparation()
        {
            container = MockHelper.InitializeContainer(new[] { typeof(IProjectionsCollector).Assembly, typeof(Element).Assembly }, new Type[0]);           
            var pool = new ResourcePool();           
            container.ComposeExportedValue(pool);
            container.ComposeExportedValue(container);
            Settings.NoCacheGarbageChecking = true;
        }

        [TestCleanup]
        public void Shutdown()
        {
            container.Dispose();
        }

        private Element rootElement;
        private Attribute attr1;
        private Attribute attr2;
        private Value val1;
        private Value val2;
        private Value val3;
        private Value val4;

        private void Initialize()
        {
            rootElement = new Element { Id = Guid.NewGuid(), Name = "Root" };
            attr1 = new Attribute { Id = Guid.NewGuid(), ElementId = rootElement.Id, Name = "Attr1" };
            attr2 = new Attribute { Id = Guid.NewGuid(), ElementId = rootElement.Id, Name = "Attr2" };
            val1 = new Value { Id = Guid.NewGuid(), Name = "Val1", AttributeId = attr1.Id };
            val2 = new Value { Id = Guid.NewGuid(), Name = "Val2", AttributeId = attr1.Id };
            val3 = new Value { Id = Guid.NewGuid(), Name = "Val3", AttributeId = attr2.Id };
            val4 = new Value { Id = Guid.NewGuid(), Name = "Val4", AttributeId = attr2.Id };          
            var cachier = container.GetExportedValue<ITypedPool>();
            cachier.Post(rootElement.Id,rootElement);
            cachier.Post(attr1.Id, attr1);
            cachier.Post(attr2.Id, attr2);
            cachier.Post(val1.Id, val1);
            cachier.Post(val2.Id, val2);
            cachier.Post(val3.Id, val3);
            cachier.Post(val4.Id, val4);



        }

        private void CheckSimple(XDocument doc,string nmspc)
        {
            Assert.AreEqual("{"+nmspc + "}Element", doc.Root.Name);
            Assert.AreEqual(rootElement.Name, doc.Root.Attribute(XName.Get("Name")).Value);

            var attrs = doc.Root.Elements(XName.Get("Attribute",nmspc)).ToArray();
            Assert.AreEqual(2, attrs.Count());
            Assert.AreEqual(attr1.Name, attrs[0].Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(attr2.Name, attrs[1].Attribute(XName.Get("Name")).Value);

            var vals1 = attrs[0].Elements(XName.Get("Value", nmspc)).ToArray();
            Assert.AreEqual(2, vals1.Count());
            Assert.AreEqual(val1.Name, vals1[0].Attribute(XName.Get("Name")).Value);
            Assert.AreEqual( val2.Name, vals1[1].Attribute(XName.Get("Name")).Value);

            var vals2 = attrs[1].Elements(XName.Get("Value",nmspc)).ToArray();
            Assert.AreEqual(2, vals2.Count());
            Assert.AreEqual(val3.Name, vals2[0].Attribute(XName.Get("Name")).Value);
            Assert.AreEqual( val4.Name, vals2[1].Attribute(XName.Get("Name")).Value);
        }

        [TestMethod]
        [TestCategory("ProjectionBuild")]
        public void SimpleQuery()
        {
            Initialize();

            var querier = container.GetExportedValue<IProjectionsCollector>();
            var data = querier.GetProjection(rootElement.Id,"Element", "http://tempuri.org/BaseScheme");
            data.Position = 0;
            var doc = XDocument.Load(data);
            CheckSimple(doc, "http://tempuri.org/BaseScheme");

        }

        private void InitializeForNamespace()
        {
            rootElement = new Element { Id = Guid.NewGuid(), Name = "Root" };
            attr1 = new Attribute { Id = Guid.NewGuid(), ElementId = rootElement.Id, Name = "NotAllowedAttr" };
            attr2 = new Attribute { Id = Guid.NewGuid(), ElementId = rootElement.Id, Name = "AllowedAttr" };
            val1 = new Value { Id = Guid.NewGuid(), Name = "AllowedVal1", AttributeId = attr1.Id };
            val2 = new Value { Id = Guid.NewGuid(), Name = "NotAllowedVal1", AttributeId = attr1.Id };
            val3 = new Value { Id = Guid.NewGuid(), Name = "AllowedVal2", AttributeId = attr2.Id };
            val4 = new Value { Id = Guid.NewGuid(), Name = "NotAllowedVal2", AttributeId = attr2.Id };

            var cachier = container.GetExportedValue<ITypedPool>();
            cachier.Post(rootElement.Id, rootElement);
            cachier.Post(attr1.Id, attr1);
            cachier.Post(attr2.Id, attr2);
            cachier.Post(val1.Id, val1);
            cachier.Post(val2.Id, val2);
            cachier.Post(val3.Id, val3);
            cachier.Post(val4.Id, val4);
        }

        private void CheckForNamespace(XDocument doc)
        {
            Assert.AreEqual("Element", doc.Root.Name.LocalName);
            Assert.AreEqual(rootElement.Name, doc.Root.Attribute(XName.Get("Name")).Value);

            var vals1 = doc.Root.Elements().ToArray();

            Assert.AreEqual(4, doc.Root.Nodes().Count());
            Assert.AreEqual(val1.Name, vals1[0].Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(val2.Name, vals1[1].Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(val3.Name, vals1[2].Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(val4.Name, vals1[3].Attribute(XName.Get("Name")).Value);

            Assert.AreEqual(1, vals1[0].Elements().Count());
            var elem1 = vals1[0].Elements().Single();
            Assert.AreEqual("Element", elem1.Name.LocalName);
            Assert.AreEqual(rootElement.Name, elem1.Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(1, elem1.Descendants().Count());

            Assert.AreEqual(1, vals1[1].Elements().Count());
            var elem2 = vals1[1].Elements().Single();
            Assert.AreEqual("Element", elem2.Name.LocalName);
            Assert.AreEqual(rootElement.Name, elem1.Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(1, elem2.Elements().Count());

            Assert.AreEqual(1, vals1[2].Elements().Count());
            var elem3 = vals1[2].Elements().Single();
            Assert.AreEqual("Element", elem3.Name.LocalName );
            Assert.AreEqual(rootElement.Name, elem1.Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(1, elem3.Elements().Count());

            Assert.AreEqual(1, vals1[3].Elements().Count());
            var elem4 = vals1[3].Elements().Single();
            Assert.AreEqual("Element", elem4.Name.LocalName);
            Assert.AreEqual(rootElement.Name, elem1.Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(1, elem4.Descendants().Count());

            var attrs = elem1.Elements().ToArray();
            Assert.AreEqual(attr2.Name, attrs[0].Attribute(XName.Get("Name")).Value);
            attrs = elem2.Elements().ToArray();
            Assert.AreEqual(attr2.Name, attrs[0].Attribute(XName.Get("Name")).Value);
            attrs = elem3.Elements().ToArray();
            Assert.AreEqual(attr2.Name, attrs[0].Attribute(XName.Get("Name")).Value);
            attrs = elem4.Elements().ToArray();
            Assert.AreEqual(attr2.Name, attrs[0].Attribute(XName.Get("Name")).Value);    
        }

        private void CheckForParametrizedName(XDocument doc)
        {
            Assert.AreEqual("Element", doc.Root.Name.LocalName);
            Assert.AreEqual(rootElement.Name, doc.Root.Attribute(XName.Get("Name")).Value);

            var vals1 = doc.Root.Elements().ToArray();

            Assert.AreEqual(4, doc.Root.Nodes().Count());
            Assert.AreEqual(val1.Name, vals1[0].Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(val2.Name, vals1[1].Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(val3.Name, vals1[2].Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(val4.Name, vals1[3].Attribute(XName.Get("Name")).Value);

            Assert.AreEqual(1, vals1[0].Elements().Count());
            var elem1 = vals1[0].Elements().Single();
            Assert.AreEqual("Element", elem1.Name.LocalName);
            Assert.AreEqual(rootElement.Name, elem1.Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(1, elem1.Descendants().Count());

            Assert.AreEqual(1, vals1[1].Elements().Count());
            var elem2 = vals1[1].Elements().Single();
            Assert.AreEqual("Element", elem2.Name.LocalName);
            Assert.AreEqual(rootElement.Name, elem1.Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(1, elem2.Elements().Count());

            Assert.AreEqual(1, vals1[2].Elements().Count());
            var elem3 = vals1[2].Elements().Single();
            Assert.AreEqual("Element", elem3.Name.LocalName);
            Assert.AreEqual(rootElement.Name, elem1.Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(1, elem3.Elements().Count());

            Assert.AreEqual(1, vals1[3].Elements().Count());
            var elem4 = vals1[3].Elements().Single();
            Assert.AreEqual("Element", elem4.Name.LocalName);
            Assert.AreEqual(rootElement.Name, elem1.Attribute(XName.Get("Name")).Value);
            Assert.AreEqual(1, elem4.Descendants().Count());

            var attrs = elem1.Elements().ToArray();
            Assert.AreEqual(attr1.Name, attrs[0].Attribute(XName.Get("Name")).Value);
            attrs = elem2.Elements().ToArray();
            Assert.AreEqual(attr1.Name, attrs[0].Attribute(XName.Get("Name")).Value);
            attrs = elem3.Elements().ToArray();
            Assert.AreEqual(attr1.Name, attrs[0].Attribute(XName.Get("Name")).Value);
            attrs = elem4.Elements().ToArray();
            Assert.AreEqual(attr1.Name, attrs[0].Attribute(XName.Get("Name")).Value);
        }

        [TestMethod]
        [TestCategory("ProjectionBuild")]
        public void NamespacedQuery()
        {
            InitializeForNamespace();
            var querier = container.GetExportedValue<IProjectionsCollector>();
            var data = querier.GetProjection(rootElement.Id, "Element", "http://tempuri.org/Namefilter");
            data.Position = 0;
            var doc = XDocument.Load(data);
            CheckForNamespace(doc);
        }


        private void CheckForGroup(XDocument doc)
        {
            dynamic elGroupTag = new XmlBuilder("ElementGroup", "http://tempuri.org/GroupScheme");
            dynamic elTag = new XmlBuilder("Element", "http://tempuri.org/GroupScheme");
            dynamic attrGroupTag = new XmlBuilder("AttributeGroup", "http://tempuri.org/GroupScheme");
            dynamic attrSubGroupTag = new XmlBuilder("AttributeSubGroup", "http://tempuri.org/GroupScheme");
            dynamic attrTag = new XmlBuilder("Attribute", "http://tempuri.org/GroupScheme");
            XDocument tdoc =
                elGroupTag(id: rootElement.Id,
                           name: "ElementGroup: " + rootElement.Id,
                           elTag: elTag(id: rootElement.Id,
                                        name: rootElement.Name,
                                        at: attrGroupTag(id: rootElement.Id,
                                                         name:"AttributeGroup: " + rootElement.Id,
                                                         ast: attrSubGroupTag(id: rootElement.Id,
                                                                              name: "AttributeSubGroup: " + rootElement.Id,
                                                             a1: attrTag(id: attr1.Id, name: attr1.Name, definitlyNoChildren: true),
                                                             a2: attrTag(id: attr2.Id, name: attr2.Name, definitlyNoChildren: true)
                                                         ))
                               )
                    );

            Assert.IsTrue(new XDocComparer().Equals(doc,tdoc));
          

        }

        [TestMethod]
        [TestCategory("ProjectionBuild")]
        public void GroupQuery()
        {
            Initialize();
            var querier = container.GetExportedValue<IProjectionsCollector>();
            var data = querier.GetProjection(rootElement.Id, "ElementGroup", "http://tempuri.org/GroupScheme");
            data.Position = 0;
            var doc = XDocument.Load(data);

            CheckForGroup(doc);
        }



        [TestMethod]
        [TestCategory("ProjectionBuild")]
        public void ParametrizedQuery()
        {
            InitializeForNamespace();

            var querier = container.GetExportedValue<IProjectionsCollector>();
            var data = querier.GetProjection(rootElement.Id, "Element", "http://tempuri.org/Namefilter",new HierarchyQuery{Query="Name=NotAllowed"});
            data.Position = 0;
            var doc = XDocument.Load(data);
            CheckForParametrizedName(doc);

            data = querier.GetProjection(rootElement.Id, "Element", "http://tempuri.org/Namefilter", new HierarchyQuery { Query = "Name=NotAllowed", AreaDepth = 1 });
            data.Position = 0;
            doc = XDocument.Load(data);

            Assert.AreEqual("Element", doc.Root.Name.LocalName);
            Assert.AreEqual(rootElement.Name, doc.Root.Attribute("Name").Value);
            Assert.AreEqual(0, doc.Root.Elements().Count());

            data = querier.GetProjection(rootElement.Id, "Element", "http://tempuri.org/Namefilter", new HierarchyQuery { Query = "Name=NotAllowed", AreaDepth = 3 });
            data.Position = 0;
            doc = XDocument.Load(data);
            CheckForParametrizedName(doc);
        }

        [TestMethod]
        [TestCategory("ProjectionBuild")]
        public void ParametrizedQueryWithMultipleGetters()
        {
            InitializeForNamespace();

            var querier = container.GetExportedValue<IProjectionsCollector>();
            var data = querier.GetProjection(rootElement.Id, "Element", "http://tempuri.org/NamefilterWithGroups", new HierarchyQuery { Query = "Name=NotAllowed" });
            data.Position = 0;
            var doc = XDocument.Load(data);
            CheckForParametrizedName(doc);

            data = querier.GetProjection(rootElement.Id, "Element", "http://tempuri.org/NamefilterWithGroups", new HierarchyQuery { Query = "Name=NotAllowed", AreaDepth = 1 });
            data.Position = 0;
            doc = XDocument.Load(data);

            Assert.AreEqual("Element", doc.Root.Name.LocalName);
            Assert.AreEqual(rootElement.Name, doc.Root.Attribute("Name").Value);
            Assert.AreEqual(0, doc.Root.Elements().Count());

            data = querier.GetProjection(rootElement.Id, "Element", "http://tempuri.org/NamefilterWithGroups", new HierarchyQuery { Query = "Name=NotAllowed", AreaDepth = 3 });
            data.Position = 0;
            doc = XDocument.Load(data);
            CheckForParametrizedName(doc);
        }
    }
}
