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
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Linq;
using Dataspace.Common.ClassesForImplementation;

namespace  Projections.Test.Data

{
    [Export(typeof(ResourcePoster))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Pure]
    public partial class ElementWriter : ResourcePoster<Element>
    {
	
	    [Import]
        private ResourcePool _pool;

        protected override void WriteResourceTyped(Guid id,Element res)
        {
           if (_pool.Elements.ContainsKey(id))
                 _pool.Elements[id] = res;
		   else _pool.Elements.Add(id,res);
            
           
        }

        protected override void DeleteResourceTyped(Guid id)
        {
            if (_pool.Elements.ContainsKey(id))
                 _pool.Elements.Remove(id);
        }
	     
    }
}


