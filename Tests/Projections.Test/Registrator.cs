
using System;
using Dataspace.Common.ClassesForImplementation;
using Hierarchies.Test.Data;
using System.ComponentModel.Composition;
using Projections.Test.Data;
using Attribute = Projections.Test.Data.Attribute;


[Export(typeof(ResourceRegistrator))]
internal class CoreResourceRegistrator:ResourceRegistrator
{

     protected override Type[] ResourceTypes { 
	    get
		  { 
		     return new[] {
                              typeof(Attribute),
                              typeof(Element),
                              typeof(Value),
                       };
         }
    }
}