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
using Dataspace.Common.ClassesForImplementation;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Linq;

namespace  Projections.Test.Data

{
    [Export(typeof(ResourceGetter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Pure]
    public partial class AttributeProvider : ResourceGetter<Attribute>
    {
	
	    [Import]
        private ResourcePool _pool;

        protected override Attribute GetItemTyped(Guid id)
        {
            try
            {
                return _pool.Attributes[id];
            }
            catch (KeyNotFoundException)
            {

                return null;
                
            }
           
        }

        protected override IEnumerable<KeyValuePair<Guid, Attribute>> GetItemsTyped(IEnumerable<Guid> id)
        {
            return id.Select(k => new KeyValuePair<Guid, Attribute>(k, _pool.Attributes[k])).ToArray();
        }
	     
    }
}


