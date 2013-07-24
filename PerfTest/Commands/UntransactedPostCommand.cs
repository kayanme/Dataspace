using System;
using Dataspace.Common.Interfaces;
using Indusoft.Test.MockresourceProviders;
using PerfTest.Classes;
using PerfTest.Metadata;

namespace PerfTest.Commands
{
    public class UntransactedPostCommand:NodeCommandBase<IGenericPool>
    {
        public override void Do(IGenericPool service)
        {
           service.Post(_name,_id,Res);
        }

        public override bool Check(Store store)
        {
            var res = store.GetResource(ResourceSpaceDescriptions.ResourceAssembly.GetType(_name), _id) as ResBase;
            return res.Payload == (Res as ResBase).Payload;
        }

        private Guid _id;

        private string _name;

        public ResBase Res { get; private set; }

        public UntransactedPostCommand(Guid id, string name,Guid? aff1,Guid? aff2,string nodeName)
        {
            _id = id;
            _name = name;
            var res = CreateResource(name);
            res.Payload = Guid.NewGuid();
            res.ResourceAffinity1 = aff1;
            res.ResourceAffinity2 = aff2;
            res.NodeAffinity = nodeName;
            Res = res;
        }
    }
}
