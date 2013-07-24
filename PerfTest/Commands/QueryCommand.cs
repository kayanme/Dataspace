using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Indusoft.Test.MockresourceProviders;
using PerfTest.Classes;
using PerfTest.Metadata;

namespace PerfTest.Commands
{
    internal sealed class QueryCommand : NodeCommandBase<IGenericPool>
    {
        private UriQuery _query;

        public string TargetName { get; private set; }

        public override void Do(IGenericPool service)
        {
           Ids = service.Get(TargetName, _query);
        }

        public override bool Check(Store store)
        {
             return true;
        }

        public IEnumerable<Guid> Ids { get; private set; }

        public QueryCommand(Guid resId, int affNum, string resName)
        {
            _query = new UriQuery {{"Affinity" + affNum, resId.ToString()}};

            if (affNum == 1)
                TargetName = ResourceSpaceDescriptions.Affinities[resName].Aff1;
            if (affNum == 2)
                TargetName = ResourceSpaceDescriptions.Affinities[resName].Aff2;
        }

        public QueryCommand(string nodeName, string resName)
        {
            _query = new UriQuery { { "Node", nodeName } };          
            TargetName = resName;
            
        }


        public QueryCommand(Guid resId, int affNum, string resName,string nodeName):this(resId,affNum,resName)
        {
            _query.Add("Node", nodeName);
            TargetName = resName;

        }

        public QueryCommand(string resName)
        {
            _query = new UriQuery ();
            TargetName = resName;

        }
    }
}
