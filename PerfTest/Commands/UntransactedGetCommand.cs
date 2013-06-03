using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Interfaces;
using Indusoft.Test.MockresourceProviders;
using PerfTest.Classes;

namespace PerfTest.Commands
{
    public class UntransactedGetCommand:NodeCommandBase<IGenericPool>
    {
        public override void Do(IGenericPool service)
        {
            _res = service.Get(_name, _id) as ResBase;
        }

        public override bool Check(Store store)
        {
            var res = store.GetResource(GetResourceType(_name), _id) as ResBase;
            return res.Payload == _res.Payload;
        }

        private Guid _id;

        private string _name;

        private ResBase _res;

        public UntransactedGetCommand(Guid id,string name)
        {
            _id = id;
            _name = name;
        }
    }
}
