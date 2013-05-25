using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Projections.Classes.Plan;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Resources.Test
{
    [TestClass]
    public class ParamNamesTest
    {
        public TestContext TestContext { get; set; }

       
        private void ParameterNamesEquality()
        {
            var args1 = (TestContext.Properties["args1"] as string).Split(',');
            var args2 = (TestContext.Properties["args2"] as string).Split(',');
            var parNames1 = new ParameterNames(args1);
            var parNames2 = new ParameterNames(args2);
            var eq = (TestContext.Properties["equals"] as string) == "yes";
            if (eq)
                Assert.AreEqual(parNames1,parNames2);
            else
            {
                Assert.AreNotEqual(parNames1,parNames2);
            }                        
        }

        [TestProperty("args1", "a1")]
        [TestProperty("args2", "a1")]
        [TestProperty("equals", "yes")]
        [TestCategory("Queries")]
        [TestMethod]
        public void ParameterNamesEquality1()
        {
            ParameterNamesEquality();
        }

        [TestProperty("args1", "a1,a2")]
        [TestProperty("args2", "a1,a2")]
        [TestProperty("equals", "yes")]
        [TestCategory("Queries")]
        [TestMethod]
        public void ParameterNamesEquality2()
        {
            ParameterNamesEquality();
        }

        [TestProperty("args1", "a1,a2")]
        [TestProperty("args2", "a1")]
        [TestProperty("equals", "no")]
        [TestCategory("Queries")]
        [TestMethod]
        public void ParameterNamesEquality3()
        {
            ParameterNamesEquality();
        }

        [TestProperty("args1", "a2,a2")]
        [TestProperty("args2", "a1,a2")]
        [TestProperty("equals", "no")]
        [TestCategory("Queries")]
        [TestMethod]
        public void ParameterNamesEquality4()
        {
            ParameterNamesEquality();
        }

        [TestProperty("args1", "")]
        [TestProperty("args2", "a1,a2")]
        [TestProperty("equals", "no")]
        [TestCategory("Queries")]
        [TestMethod]
        public void ParameterNamesEquality5()
        {
            ParameterNamesEquality();
        }

        [TestProperty("args1", "")]
        [TestProperty("args2", "")]
        [TestProperty("equals", "yes")]
        [TestCategory("Queries")]
        [TestMethod]
        public void ParameterNamesEquality6()
        {
            ParameterNamesEquality();
        }

        [TestProperty("args1", "A1")]
        [TestProperty("args2", "a1")]
        [TestProperty("equals", "yes")]
        [TestCategory("Queries")]
        [TestMethod]
        public void ParameterNamesEqualityWithCase1()
        {
            ParameterNamesEquality();
        }

        [TestProperty("args1", "A2,a1")]
        [TestProperty("args2", "A1,a2")]
        [TestProperty("equals", "yes")]
        [TestCategory("Queries")]
        [TestMethod]
        public void ParameterNamesEqualityWithCase2()
        {
            ParameterNamesEquality();
        }
    }
}
