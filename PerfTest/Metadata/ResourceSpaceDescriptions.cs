using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PerfTest.Metadata
{
    public static class ResourceSpaceDescriptions
    {
        public const int Count = 50;

        public static Assembly ResourceAssembly;

        public struct ResAff
        {
            public string Aff1;
            public string Aff2;
        }

        public readonly static Dictionary<string,ResAff> Affinities = new Dictionary<string,ResAff>();
    }
}
