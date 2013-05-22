using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using Dataspace.Common.Hierarchies;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Projections.Classes;
using Dataspace.Common.Projections.Classes.Descriptions;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.Projections.Services;
using Dataspace.Common.Utility;
using Projections.Test.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Projections.Test.UnitTests
{

    [TestClass]
    public sealed class PlanProcessorTests
    {
        private class TestCommendationCollection1 : CommendationCollection
        {
            public override IEnumerable<FrameNodeGroup> GetNewGroups(IEnumerable<FrameNodeGroup> groups)
            {
                var g = groups.ToArray();
                if (g.Count() == 1 && g[0].MatchedElement.Name == "Element")
                {
                    var nodes = _attrIds.Select(
                        k =>
                        new FrameNode(k, _attribute, 1,
                                      new Dictionary<string, object>())).ToArray();
                    g[0].Nodes[0].ChildNodes = nodes;
                    return new[]
                               {
                                   new FrameNodeGroup(_attribute,
                                                      nodes,
                                                      new ParameterNames())
                               };
                }
                if (g.Count() == 1 && g[0].MatchedElement.Name == "Attribute")
                {
                    foreach(var node in  g[0].Nodes)
                       node.ChildNodes = new FrameNode[0];
                    return new FrameNodeGroup[0];
                }

                Assert.Fail("Неверный шаг");
                throw new InvalidOperationException();
            }

           
            private ProjectionElement _attribute;


            private Guid[] _attrIds;
            public TestCommendationCollection1(ProjectionElement attribute, Guid[] attrIds)
            {
                _attrIds = attrIds;
                
                _attribute = attribute;
            }
        }

        [TestMethod]
        [TestCategory("Projections")]
        public void SimplePlan()
        {
            var catalog = new AggregateCatalog(
                                    new AssemblyCatalog(typeof (ITypedPool).Assembly),
                                    new AssemblyCatalog(typeof (Element).Assembly));
            var container = new CompositionContainer(catalog);
            container.ComposeExportedValue(container);
            var cachier = container.GetExportedValue<ITypedPool>();
            var rootElement = new Element { Id = Guid.NewGuid(), Name = "Root" };
            var attr1 = new Data.Attribute { Id = Guid.NewGuid(), ElementId = rootElement.Id, Name = "Attr1" };
            var attr2 = new Data.Attribute { Id = Guid.NewGuid(), ElementId = rootElement.Id, Name = "Attr2" };
            cachier.Post(rootElement.Id, rootElement);
            cachier.Post(attr1.Id, attr1);
            cachier.Post(attr2.Id, attr2);


            var sAttr = new ResourceProjectionElement
            {
                Name = "Attribute",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Attribute",
                Namespace = "Test"
            };
            var sElement = new ResourceProjectionElement
            {
                Name = "Element",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Element",
                Namespace = "Test",
            };

            var steps = new TestCommendationCollection1(sAttr, new[] { attr1.Id, attr2.Id });
            var plan = new TestFramingPlan(sElement, steps);
            var builder = container.GetExportedValue<ProjectionBuilder>();
            var writer = new TestWriter();
            var sb = builder.RealisePlan(plan, rootElement.Id, "Test", 2, new Streamer<StringBuilder>(writer), new Dictionary<string, object>());

            var expWriter = new TestWriter();
            var expSb = expWriter.Open("Test");
            expWriter.StartWritingNode("Element", rootElement.Id);
            expWriter.WriteAttribute("Name", rootElement.Name);
            expWriter.StartWritingNode("Attribute", attr1.Id);
            expWriter.WriteAttribute("Name", attr1.Name);
            expWriter.WriteAttribute("DefinitlyNoChildren", "True");
            expWriter.EndWritingNode();
            expWriter.StartWritingNode("Attribute", attr2.Id);
            expWriter.WriteAttribute("Name", attr2.Name);
            expWriter.WriteAttribute("DefinitlyNoChildren", "True");
            expWriter.EndWritingNode();
            expWriter.EndWritingNode();
            Assert.AreEqual(expSb.ToString(), sb.ToString());
        }
    }
}
