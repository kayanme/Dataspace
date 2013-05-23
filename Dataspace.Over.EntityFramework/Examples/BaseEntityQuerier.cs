 //*********************************************************
//
//    
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
 using System;
using System.Collections.Generic;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Linq;

namespace  Dataspace.Over.EntityFramework.Examples

{
    [Export(typeof(ResourceQuerier))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Pure]
    public partial class BaseEntityQuerier : ResourceQuerier<BaseEntity> 
    {
	#pragma warning disable 0649
	      [Import]
		  private QueryProcessor _queryProc;
#pragma warning restore 0649
	      [IsQuery]
		  public IEnumerable<Guid> GetAllOfThis()
		  {
			  return _queryProc.GetItems<BaseEntity>(k=>true).Select(k=>k.Id).ToArray();
		  }

   
	}

}
