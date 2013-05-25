using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;



namespace Resources.Test.Providers
{
    [Export(typeof(IResourceQuerierFactory))]
    internal class Querier:IResourceQuerierFactory
    {
              

        public FormedQuery CreateQuerier(string type, string nmspc, string[] args)
        {
            return k=> new[] {Guid.Empty};
        }
    }
}
