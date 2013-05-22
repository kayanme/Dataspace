using System.Collections.Generic;
using Dataspace.Common.ClassesForImplementation;

namespace Dataspace.Common.Projections.Classes
{
    internal class Relation
    {
        public ProjectionElement ParentElement;

        public ProjectionElement ChildElement;

        public IEnumerable<ResourceQuerier.FuncWithSortedArgs> Queries = new ResourceQuerier.FuncWithSortedArgs[0];

        public IEnumerable<ResourceQuerier.SeriesFuncWithSortedArgs> SeriaQueries =
            new ResourceQuerier.SeriesFuncWithSortedArgs[0];

        public IEnumerable<ResourceQuerier.FuncWithSortedArgs> QueriesFromPhysicalSpace = new ResourceQuerier.FuncWithSortedArgs[0];

        public IEnumerable<ResourceQuerier.SeriesFuncWithSortedArgs> SeriaQueriesFromPhysicalSpace = new ResourceQuerier.SeriesFuncWithSortedArgs[0];

        public bool HasTrivialQuery { get { return Queries == null 
                                                && SeriaQueries == null 
                                                && QueriesFromPhysicalSpace == null
                                                && SeriaQueriesFromPhysicalSpace == null; } }
    }
}
