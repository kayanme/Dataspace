using System;
using Dataspace.Common.Interfaces;
using Indusoft.Test.MockresourceProviders;
using PerfTest.Classes;
using PerfTest.Metadata;

namespace PerfTest.Commands
{
    public class UntransactedDeleteCommand:NodeCommandBase<IGenericPool>
    {
        public override void Do(IGenericPool service)
        {
           service.Post(_name,_id,null);
        }

        public override bool Check(Store store)
        {
            var res = store.GetResource(ResourceSpaceDescriptions.ResourceAssembly.GetType(_name), _id) as ResBase;
            return res == null;
        }

        private Guid _id;

        private string _name;      

        public UntransactedDeleteCommand(Guid id, string name)
        {
            _id = id;
            _name = name;          
        }
    }
}
