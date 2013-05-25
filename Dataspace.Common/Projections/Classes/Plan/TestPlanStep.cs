using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.ServiceResources;

namespace Dataspace.Common.Projections.Classes.Descriptions
{
    internal sealed class TestPlanStep : PlanStep
    {
        public readonly Query Query;

        public TestPlanStep(ProjectionElement parent, ProjectionElement child, ParameterNames pars, ParameterNames allPars, int pGroup, Query query)
            : base(parent, child, pars, null, allPars, pGroup, false)
        {
            Query = query;
        }
    }
}
