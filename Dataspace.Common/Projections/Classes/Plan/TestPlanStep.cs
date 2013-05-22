using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Projections.Classes.Plan;

namespace Dataspace.Common.Projections.Classes.Descriptions
{
    internal sealed class TestPlanStep : PlanStep
    {
        public readonly ResourceQuerier.BaseFuncWithSortedArgs Query;

        public TestPlanStep(ProjectionElement parent, ProjectionElement child, ParameterNames pars, ParameterNames allPars, int pGroup, ResourceQuerier.BaseFuncWithSortedArgs query)
            : base(parent, child, pars, null, allPars, pGroup, false)
        {
            Query = query;
        }
    }
}
