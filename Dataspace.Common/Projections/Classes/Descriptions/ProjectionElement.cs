using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;


namespace Dataspace.Common.Projections.Classes
{
    [DebuggerDisplay("{Name}")]
    internal class ProjectionElement
    {
        public string Name;
        public string Namespace;
        public string SchemeType;
        public PropertiesProvider.PropertyFiller PropertyFiller;
        public FillingInfo FillingInfo;

        public List<Relation> DownRelations = new List<Relation>();
        public List<Relation> UpRelations = new List<Relation>();

        public override string ToString()
        {
            return Name;
        }
    }
}
