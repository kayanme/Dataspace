using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Dataspace.Common.Utility
{
    public sealed class XDocComparer:IEqualityComparer<XDocument>
    {

        private bool ElementsEqual(XElement elem1,XElement elem2)
        {
            Func<string, string, bool> equals =
                (s1, s2) => string.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase);
            if (elem1.Name != elem2.Name)
                return false;

            var attrs1 = elem1.Attributes().Where(k=>!k.IsNamespaceDeclaration);
            var attrs2 = elem2.Attributes().Where(k => !k.IsNamespaceDeclaration); 
            if (attrs1.Count() != attrs2.Count())
                return false;

            var equalities =
              attrs1.OrderBy(k => k.Name.LocalName)
                    .Zip(attrs2.OrderBy(k => k.Name.LocalName),
                        (a, b) => equals(a.Name.LocalName, b.Name.LocalName)
                               && equals(a.Value, b.Value));
            return equalities.All(k => k);
        }

        private bool ElementsContentEqual(XElement elem1, XElement elem2)
        {
            var ec1 = elem1.Elements();
            var ec2 = elem2.Elements();
            if (ec1.Count() != ec2.Count())
                return false;

            foreach (var xElement in ec1)
            {
                var xEl2 = ec2.FirstOrDefault(k => ElementsEqual(xElement, k));
                if (xEl2 == null)
                    return false;
                if (!ElementsContentEqual(xElement, xEl2))
                    return false;
            }

            return true;
        }

        public bool Equals(XDocument x, XDocument y)
        {
            if (!ElementsEqual(x.Root, y.Root))
                return false;

            return ElementsContentEqual(x.Root, y.Root);
        }

        public int GetHashCode(XDocument obj)
        {
            throw new NotImplementedException();
        }
    }
}
