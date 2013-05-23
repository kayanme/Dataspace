
using System;
using System.ComponentModel.Composition;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Over.EntityFramework.Examples;

[Export(typeof(ResourceRegistrator))]
internal class CoreResourceRegistrator:ResourceRegistrator
{

     protected override Type[] ResourceTypes { 
	    get
		  { 
		     return new[] {
                              typeof(BaseEntity),
                       };
         }
    }
}