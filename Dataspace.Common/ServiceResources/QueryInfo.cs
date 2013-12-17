using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Dataspace.Common.Projections.Classes.Plan;

namespace Dataspace.Common.ServiceResources
{
    public sealed class QueryInfo
    {
        public Guid ResourceKey { get;private set; }
        public ParameterNames Arguments { get;private set; }
        public string Namespace { get; private set; }
        public MethodInfo CallingMethod { get; private set; }
        public int ArgCount { get { return Arguments.Count; } }

        internal QueryInfo(Guid key,ParameterNames args,string nmspc,MethodInfo methodInfo)
        {
            ResourceKey = key;
            Arguments = args;
            Namespace = nmspc;
            CallingMethod = methodInfo;
            foreach (var s in Arguments)
            {
                string.Intern(s);
            }
        }
    }
}
