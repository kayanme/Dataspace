using System;
using System.Collections.Generic;
using System.Linq;
using Dataspace.Common.Data;

namespace Server.Modules.HierarchiesModule.Queries
{
    internal class ProcessorQuery
    {
               
        public int LeftDepth { get; set; }
    
        public List<Guid> RemainingElemens { get; private set; }

        public int MaxItems { get; private set; }

        public UriQuery Query { get; set; }


        public ProcessorQuery(HierarchyQuery query)
        {
            if (query != null)
            {
                LeftDepth = query.AreaDepth == 0?-1:query.AreaDepth;
                RemainingElemens = (query.RequiredElemens??new Guid[0]).ToList();
                MaxItems = query.MaxItems == 0 ? int.MaxValue : query.MaxItems;
                if (query.Query!=null)
                   Query = new UriQuery(query.Query);
            }
            else
            {
                LeftDepth = -1;
                RemainingElemens = new List<Guid>();
                MaxItems = int.MaxValue;
              
            }
        }
    }
}
