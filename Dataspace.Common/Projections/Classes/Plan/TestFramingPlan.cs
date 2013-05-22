using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Projections.Classes.Descriptions;

namespace Dataspace.Common.Projections.Classes
{
    internal sealed class TestFramingPlan : FramingPlan
    {
        public TestFramingPlan(ProjectionElement root)
            : base(root)
        {

        }

        public TestFramingPlan(ProjectionElement root, CommendationCollection collection)
            : base(root)
        {
            _commendations = collection;
        }


        public struct TestPlanVew
        {
            public struct ParChild
            {
                public ProjectionElement Parent;
                public ProjectionElement Child;
            }

            public readonly TestDescription Test;
            public readonly IEnumerable<ParChild> Processings;

            public TestPlanVew(TestDescription test, IEnumerable<ParChild> processings)
            {
                Debug.Assert(test != null);
                Debug.Assert(processings != null);
                Test = test;
                Processings = processings;
            }
        }

        protected override PlanStep CreateStep(ProjectionElement parent, ProjectionElement child, Plan.ParameterNames pars, Plan.ParameterNames allPars, Utility.Accumulator<FrameNode, IEnumerable<Guid>> provider, int priorGroup, ResourceQuerier.BaseFuncWithSortedArgs func)
        {
            var step = new TestPlanStep(parent, child, pars, allPars,priorGroup,func);
            return step;
        }

       


        public IEnumerable<PlanStep> ReturnPlanningResults()
        {
            return
                _commendations.PossibleSteps;
        }

     

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
