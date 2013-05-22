
 
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

namespace  Projections.Test.Data

{
    [Export]
    public class ResourcePool 
    {

         internal Dictionary<Guid,Attribute>  Attributes = new Dictionary<Guid,Attribute>();   
        internal Dictionary<Guid,Element>  Elements = new Dictionary<Guid,Element>();   
        internal Dictionary<Guid,Value>  Values = new Dictionary<Guid,Value>();   
      
   }
	
	  
}
