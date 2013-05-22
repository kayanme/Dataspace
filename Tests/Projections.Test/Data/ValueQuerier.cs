 //*********************************************************
//
//    Copyright (c) Microsoft. All rights reserved.
//    This code is licensed under the Microsoft Public License.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
 using System;
using System.Collections.Generic;
using Dataspace.Common;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Linq;
 using Dataspace.Common.Attributes;
 using Dataspace.Common.ClassesForImplementation;

namespace  Projections.Test.Data

{
    [Export(typeof(ResourceQuerier))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Pure]
    public partial class ValueQuerier : ResourceQuerier<Value> 
    {
	
	      [Import]
		  private QueryProcessor _queryProc;

	      [IsQuery]
		  public IEnumerable<Guid> GetAllOfThis()
		  {
			  return _queryProc.GetItems<Value>(k=>true).Select(k=>k.Id).ToArray();
		  }

   
	}

}
