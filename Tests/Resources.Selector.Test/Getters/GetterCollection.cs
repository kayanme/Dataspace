using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;

namespace Resources.Selector.Test.Getters
{
  
   //[Export(typeof(ResourceGetter))]
   //public sealed class SimpleGet:ResourceGetter<TestResource>
   //{
   //    protected override TestResource GetItemTyped(Guid id)
   //    {
   //        return new TestResource {GetStyle = "None"};
   //    }
   //}

   [Export(typeof(ResourceGetter))]
   [ActivationSwitch(SwitchX.X1)]
   [ActivationSwitch("X","X1")]
   public sealed class X1Get : ResourceGetter<TestResource>
   {
       protected override TestResource GetItemTyped(Guid id)
       {
           return new TestResource { GetStyle = "X1" };
       }
   }

   [Export(typeof(ResourceGetter))]
   [ActivationSwitch(SwitchX.X2)]
   [ActivationSwitch("X", "X2")]
   public sealed class X2Get : ResourceGetter<TestResource>
   {
       protected override TestResource GetItemTyped(Guid id)
       {
           return new TestResource { GetStyle = "X2" };
       }
   }

   [Export(typeof(ResourceGetter))]
   [ActivationSwitch(SwitchX.X1)]
   [ActivationSwitch(SwitchY.Y1)]
   [ActivationSwitch("X", "X1")]
   [ActivationSwitch("Y", "X1")]
   public sealed class X1Y1Get : ResourceGetter<TestResource>
   {
       protected override TestResource GetItemTyped(Guid id)
       {
           return new TestResource { GetStyle = "X1Y1" };
       }
   }

   [Export(typeof(ResourceGetter))]
   [ActivationSwitch(SwitchX.X2)]
   [ActivationSwitch(SwitchY.Y1)]
   [ActivationSwitch("X", "X2")]
   [ActivationSwitch("Y", "X1")]
   public sealed class X2Y1Get : ResourceGetter<TestResource>
   {
       protected override TestResource GetItemTyped(Guid id)
       {
           return new TestResource { GetStyle = "X2Y1" };
       }
   }
}
