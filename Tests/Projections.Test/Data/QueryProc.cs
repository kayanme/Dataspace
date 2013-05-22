
 
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
using System.Linq.Expressions;
using Dataspace.Common.ClassesForImplementation;

namespace  Projections.Test.Data

{
    [Export(typeof(QueryProcessor))]
    public class QueryProc : QueryProcessor
    {
#pragma warning disable 0649
 	   [Import]
	   private ResourcePool _resourcePool;
#pragma warning restore 0649
       public override IEnumerable<T> GetItems<T>(Expression<Func<T, bool>> predicate) 
	   {
        if (typeof(T) == typeof(Attribute))
	     {
		   return  _resourcePool.Attributes.Select(k=>k.Value).OfType<T>().Where(predicate.Compile()).ToArray();
		 }
       if (typeof(T) == typeof(Element))
	     {
		   return  _resourcePool.Elements.Select(k=>k.Value).OfType<T>().Where(predicate.Compile()).ToArray();
		 }
       if (typeof(T) == typeof(Value))
	     {
		   return  _resourcePool.Values.Select(k=>k.Value).OfType<T>().Where(predicate.Compile()).ToArray();
		 }
      
       throw new ArgumentException();
        }
   }
	
	  
}
