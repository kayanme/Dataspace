using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Indusoft.Test.MockresourceProviders;
using PerfTest.Classes;

namespace PerfTest.Commands
{
    public class UntransactedGetManyCommand:NodeCommandBase<IGenericPool>
    {
        public override void Do(IGenericPool service)
        {
            Res =
            _resDescr.Select(k => new {k, v = service.GetLater(k.ResourceName, k.ResourceKey)})
                .ToArray()
                .ToDictionary(k => k.k, k => k.v.Value as ResBase);
        }

        public override bool Check(Store store)
        {
            foreach (var resBase in Res)
            {
                var res = store.GetResource(GetResourceType(resBase.Key.ResourceName),
                                            resBase.Key.ResourceKey) as ResBase;
                if (res == null || resBase.Value == null)
                {
                    if (!(res == null && Res == null))
                        return false;
                }
                else if (res.Payload == resBase.Value.Payload)
                {
                    return false;
                }
             
            }
            return true;
        }

        private IEnumerable<ResourceDescription> _resDescr;        

        public Dictionary<ResourceDescription,ResBase> Res { get; private set; }

        public UntransactedGetManyCommand(IEnumerable<ResourceDescription> resDescr)
        {
            _resDescr = resDescr;
        }
    }
}
