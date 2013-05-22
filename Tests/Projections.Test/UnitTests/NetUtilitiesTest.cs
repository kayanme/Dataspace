using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Projections.Classes;

using Projections.Test.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Dataspace.Common.Projections.Classes.Utilities;

namespace Projections.Test.UnitTests
{

    [TestClass]
    public sealed class NetUtilitiesTest
    {
        [TestMethod]
        [TestCategory("Projections")]
        public void GettersCountTest()
        {
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

            var sValue = new ResourceProjectionElement
            {
                Name = "Value",
                FillingInfo = new FillingInfo { { "Name", FillingInfo.FillType.Native } },
                ResourceType = "Element",
                Namespace = "Test",
            };

           

            var relation = new Relation { ChildElement = sAttr, ParentElement = sElement };
            sAttr.UpRelations.Add(relation);
            sElement.DownRelations.Add(relation);

            var relation2 = new Relation { ChildElement = sValue, ParentElement = sElement };
            sValue.UpRelations.Add(relation2);
            sElement.DownRelations.Add(relation2);

            relation.SeriaQueries = new[]
                                   {
                                       new ResourceQuerier.SeriesFuncWithSortedArgs(
                                           "Element",
                                           new string[0],
                                           (e, k) => new[] {new KeyValuePair<Guid, IEnumerable<Guid>>(Guid.Empty,new Guid[0])},
                                           (e, k) => new[] {new KeyValuePair<Guid, IEnumerable<Guid>>(Guid.Empty,new Guid[0])},
                                           typeof (Data.Attribute),
                                           new Func<string, object>[0])
                                   };

            Assert.AreEqual(1, sElement.MultipleGetterCount(new[] { "a", "b" }));
            Assert.AreEqual(1, sElement.MultipleGetterCount(new string[0]));
            Assert.AreEqual(1, sElement.MultipleGetterCount(new[] { "a", "b", "c" }));

            relation.Queries = new[]
                                   {
                                       new ResourceQuerier.FuncWithSortedArgs(new[]{"Element","a","b"},
                                                                                        (string[] e) => new[] {Guid.Empty},
                                                                                        (object[] e) => new[] {Guid.Empty},
                                                                                       typeof (Data.Attribute),
                                                                                       new Func<string, object>[]{ k2=>k2,k2=>k2,k2=>k2})
                                   };

            Assert.AreEqual(0,sElement.MultipleGetterCount(new[] { "a", "b" }));
            Assert.AreEqual(1, sElement.MultipleGetterCount(new string[0]));
            Assert.AreEqual(1, sElement.MultipleGetterCount(new[] { "a", "b", "c" }));

            relation.SeriaQueriesFromPhysicalSpace = new[]
                                   {
                                      new ResourceQuerier.SeriesFuncWithSortedArgs("Element",new[]{"a","b"},
                                                                                        (e, k) => new[] {new KeyValuePair<Guid, IEnumerable<Guid>>(Guid.Empty,new Guid[0])},
                                                                                          (e, k) => new[] {new KeyValuePair<Guid, IEnumerable<Guid>>(Guid.Empty,new Guid[0])},
                                                                                       typeof (Data.Attribute),
                                                                                        new Func<string, object>[]{ k2=>k2, k2=>k2})
                                   };
            Assert.AreEqual(1, sElement.MultipleGetterCount(new[] { "a", "b","c" }));
            Assert.AreEqual(0, sElement.MultipleGetterCount(new[] { "a", "b" }));
            Assert.AreEqual(1, sElement.MultipleGetterCount(new string[0]));
            relation.Queries = new ResourceQuerier.FuncWithSortedArgs[0];

            Assert.AreEqual(1, sElement.MultipleGetterCount(new[] { "a", "b", "c" }));
            Assert.AreEqual(1, sElement.MultipleGetterCount(new[] { "a", "b" }));
            Assert.AreEqual(1, sElement.MultipleGetterCount(new string[0]));

            relation2.SeriaQueries = new[]
                                   {
                                       new ResourceQuerier.SeriesFuncWithSortedArgs("Element",new string[0],
                                                                                    (e, k) => new[] {new KeyValuePair<Guid, IEnumerable<Guid>>(Guid.Empty,new Guid[0])},
                                                                                      (e, k) => new[] {new KeyValuePair<Guid, IEnumerable<Guid>>(Guid.Empty,new Guid[0])},
                                                                                    typeof (Value),
                                                                                    new Func<string, object>[0])
                                   };

            Assert.AreEqual(2, sElement.MultipleGetterCount(new[] { "a", "b", "c" }));
            Assert.AreEqual(2, sElement.MultipleGetterCount(new[] { "a", "b" }));
            Assert.AreEqual(2, sElement.MultipleGetterCount(new string[0]));

            relation.Queries = new[]
                                   {
                                       new ResourceQuerier.FuncWithSortedArgs(new[]{"Element","a","b"},
                                                                                        (string[] e) => new[] {Guid.Empty},
                                                                                        (object[] e) => new[] {Guid.Empty},
                                                                                       typeof (Data.Attribute),
                                                                                      new Func<string, object>[]{ k2=>k2,k2=>k2,k2=>k2})
                                   };


            Assert.AreEqual(2, sElement.MultipleGetterCount(new[] { "a", "b", "c" }));
            Assert.AreEqual(1, sElement.MultipleGetterCount(new[] { "a", "b" }));
            Assert.AreEqual(2, sElement.MultipleGetterCount(new string[0]));
        }

    }
}
