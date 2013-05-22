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
    [Export(typeof(ResourceQuerier))]
    internal sealed class Querier:ResourceQuerier<TestResource>
    {
       
        [IsQuery]
        [ActivationSwitch(SwitchX.X1)]
        [ActivationSwitch("X", "X1")]
        public IEnumerable<Guid> X1()
        {
            return new Guid[1];
        }

        [IsQuery]
        [ActivationSwitch(SwitchX.X2)]
        [ActivationSwitch("X", "X2")]
        public IEnumerable<Guid> X2()
        {
            return new Guid[2];
        }

        [IsQuery]
        [ActivationSwitch(SwitchX.X1)]
        [ActivationSwitch(SwitchY.Y1)]
        [ActivationSwitch("X", "X1")]
        [ActivationSwitch("Y", "Y1")]
        public IEnumerable<Guid> X1Y1()
        {
            return new Guid[3];
        }

        [IsQuery]
        [ActivationSwitch(SwitchX.X1)]
        [ActivationSwitch(SwitchY.Y2)]
        [ActivationSwitch("X", "X1")]
        [ActivationSwitch("Y", "Y2")]
        public IEnumerable<Guid> X1Y2()
        {
            return new Guid[4];
        }

    }
}
