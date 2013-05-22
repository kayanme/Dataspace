using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Projections.Classes;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.Projections.Classes.Plan;
using Getter=System.Func<System.Collections.Generic.IEnumerable<System.Guid>,
                         System.Collections.Generic.Dictionary<string,object>,
                         System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Guid, 
                                                                                                        System.Collections.Generic.IEnumerable<System.Guid>>>>;

namespace Dataspace.Common.Projections.Services.PlanBuilding
{
    internal class SecondaryFrameNode
    {
        public ProjectionElement Parent;
        public ProjectionElement Current;
        public Getter Getter;
        public ResourceQuerier.BaseFuncWithSortedArgs Query;
        //ключ - имя аргумента, значение - имя целевого параметра в запросе. В автоматическом построении всегда будут совпадать
        public ParametersMapping UsingParameters;
        public ParameterNames AllParameters;
        public int PriorityGroup;

        private class Comparer : IEqualityComparer<SecondaryFrameNode>
        {
            public bool Equals(SecondaryFrameNode x, SecondaryFrameNode y)
            {
                Debug.Assert(x.UsingParameters != null && y.UsingParameters != null);
                return x.Parent == y.Parent && x.Current == y.Current && x.UsingParameters.Equals(y.UsingParameters);
            }

            public int GetHashCode(SecondaryFrameNode obj)
            {
                unchecked
                {
                    return obj.Parent.GetHashCode() + obj.Current.GetHashCode() + obj.UsingParameters.GetHashCode();
                }

            }
        }

        public static IEqualityComparer<SecondaryFrameNode> EqualityComparer 
        {
            get
            {
                return new Comparer();
            }
    }
}

}
