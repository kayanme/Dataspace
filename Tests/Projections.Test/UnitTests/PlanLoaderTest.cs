using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Dataspace.Common.Hierarchies;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Projections;
using Dataspace.Common.Projections.Classes.Descriptions;
using Dataspace.Common.Projections.Services;
using Hierarchies.Test.Data;
using Projections.Test;
using Projections.Test.Data;
using Server.Modules.HierarchiesModule;
using Testhelper;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Dataspace.Common.Projections.Classes;
using Attribute = Projections.Test.Data.Attribute;

namespace Hierarchies.Test
{
    /// <summary>
    /// Summary description for PlanRealiserTest
    /// </summary>
    [TestClass]
    public class PlanRealiserTest
    {
        private CompositionContainer container;
        [TestInitialize]
        public void Preparation()
        {
            container = MockHelper.InitializeContainer(new[] { typeof(IProjectionsCollector).Assembly, typeof(Element).Assembly }, new Type[0]);
            var pool = new ResourcePool();
            var writer = new TestWriter();
            container.ComposeExportedValue(container);
            container.ComposeExportedValue(pool);
            container.ComposeExportedValue(writer);
            container.GetExportedValue<ICacheServicing>().Initialize();
        }

     

        [TestMethod]
        [TestCategory("Projections")]
        public void Scheme1LoaderTest()
        {

            var storage = container.GetExportedValue<ProjectionStorage>();
         
            var element = storage.FindElement("Element", "http://tempuri.org/BaseScheme");
            Assert.IsNotNull(element);
            Assert.AreEqual("Element",element.Name);
            Assert.AreEqual("http://tempuri.org/BaseScheme", element.Namespace);
            Assert.AreEqual(FillingInfo.FillType.Native, element.FillingInfo.GetFillType("Name"));
            Assert.IsNull(element.PropertyFiller);
            Assert.AreEqual(1, element.DownRelations.Count);
            var relation = element.DownRelations[0];
            var attribute = relation.ChildElement;
         
            Assert.AreEqual(element,relation.ParentElement);
            Assert.AreEqual("Attribute", attribute.Name);
            Assert.AreEqual("http://tempuri.org/BaseScheme", attribute.Namespace);
            Assert.AreEqual(1, relation.Queries.Count());            
        }

        [TestMethod]
        [TestCategory("Projections")]
        public void Scheme2LoaderTest()
        {
            container.GetExportedValue<ProjectionBuilder>();
            var storage = container.GetExportedValue<ProjectionStorage>();
            var element = storage.FindElement("Element", "http://tempuri.org/Namefilter");
            Assert.IsNotNull(element);
            Assert.AreEqual("Element", element.Name);
            Assert.AreEqual("http://tempuri.org/Namefilter", element.Namespace);
            Assert.AreEqual(FillingInfo.FillType.Native, element.FillingInfo.GetFillType("Name"));
            Assert.IsNull(element.PropertyFiller);
            Assert.AreEqual(1, element.DownRelations.Count);
            var relation = element.DownRelations[0];
            Assert.IsFalse(relation.HasTrivialQuery);
            var value = storage.FindElement("Value", "http://tempuri.org/Namefilter");
            Assert.AreEqual(element, relation.ParentElement);
            Assert.AreEqual(value, relation.ChildElement);
            value = relation.ChildElement;
            Assert.AreEqual("Value", value.Name);
            Assert.AreEqual(1, value.DownRelations.Count);
            Assert.AreEqual(1, relation.Queries.Count());
            
            value = storage.FindElement("Value", "http://tempuri.org/Namefilter");
            Assert.AreEqual(1,value.DownRelations.Count);
            Assert.AreEqual("Element", value.DownRelations[0].ChildElement.Name);
            Assert.AreEqual(1, value.DownRelations[0].ChildElement.DownRelations.Count);
            Assert.AreEqual("Attribute", value.DownRelations[0].ChildElement.DownRelations[0].ChildElement.Name);
            Assert.AreEqual(3, value.DownRelations[0].ChildElement.DownRelations[0].Queries.Count());
        }


        [TestMethod]
        [TestCategory("Projections")]
        public void NonResourceAndResourceRelationTest()
        {
            container.GetExportedValue<ProjectionBuilder>();
            var storage = container.GetExportedValue<ProjectionStorage>();
            var element = storage.FindElement("ElementGroup", "http://tempuri.org/GroupScheme");
            var attribute = element.DownRelations.First()
                      .ChildElement.DownRelations.First()
                      .ChildElement.DownRelations.First()
                      .ChildElement.DownRelations.First();
            Assert.IsTrue(attribute.Queries.Any());
        }

        [TestMethod]
        [TestCategory("Projections")]
        public void Scheme3LoaderTest()
        {
            container.GetExportedValue<ProjectionBuilder>();
            var storage = container.GetExportedValue<ProjectionStorage>();
            var elementGroup = storage.FindElement("ElementGroup", "http://tempuri.org/GroupScheme");
            Assert.IsNotNull(elementGroup);
            Assert.AreEqual("ElementGroup", elementGroup.Name);
            Assert.AreEqual("http://tempuri.org/GroupScheme", elementGroup.Namespace);
            Assert.AreEqual(FillingInfo.FillType.ByFiller, elementGroup.FillingInfo.GetFillType("Name"));
            Assert.IsNotNull(elementGroup.PropertyFiller);
            Assert.AreEqual(1, elementGroup.DownRelations.Count);
            var relation = elementGroup.DownRelations[0];
            Assert.IsFalse(relation.HasTrivialQuery);
            var element = relation.ChildElement;
            Assert.AreEqual("Element", element.Name);
            Assert.AreEqual("http://tempuri.org/GroupScheme", element.Namespace);
            Assert.AreEqual(FillingInfo.FillType.Native, element.FillingInfo.GetFillType("Name"));
            Assert.AreEqual(1, element.DownRelations.Count);
            Assert.IsTrue(element.DownRelations[0].HasTrivialQuery);
            var attrGroup = element.DownRelations[0].ChildElement;
            Assert.AreEqual("AttributeGroup", attrGroup.Name);
            Assert.AreEqual(FillingInfo.FillType.ByFiller, attrGroup.FillingInfo.GetFillType("Name"));
            Assert.IsTrue(attrGroup.DownRelations[0].HasTrivialQuery);

            var attrSubGroup = attrGroup.DownRelations[0].ChildElement;
            Assert.AreEqual("AttributeSubGroup", attrSubGroup.Name);
            Assert.AreEqual(FillingInfo.FillType.ByFiller, attrSubGroup.FillingInfo.GetFillType("Name"));
            Assert.IsFalse(attrSubGroup.DownRelations[0].HasTrivialQuery);

            var attr = attrSubGroup.DownRelations[0].ChildElement;
            Assert.AreEqual("Attribute", attr.Name);
            Assert.AreEqual(FillingInfo.FillType.Native, attr.FillingInfo.GetFillType("Name"));
            Assert.AreEqual(0, attr.DownRelations.Count);

        }

     
    }
}
