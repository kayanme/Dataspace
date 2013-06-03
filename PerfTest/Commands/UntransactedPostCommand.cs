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
    public class UntransactedPostCommand:NodeCommandBase<IGenericPool>
    {
        public override void Do(IGenericPool service)
        {
           service.Post(_name,_id,_res);
        }

        public override bool Check(Store store)
        {
            var res = store.GetResource(CacheNode.ResourceAssembly.GetType(_name), _id) as ResBase;
            return res.Payload == (_res as ResBase).Payload;
        }

        private Guid _id;

        private string _name;

        private object _res;

        public UntransactedPostCommand(Guid id, string name)
        {
            _id = id;
            _name = name;
            var res = CreateResource(name);
            res.Payload = Guid.NewGuid();
            _res = res;
        }
    }
}
