using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Dataspace.Common.Projections.Classes.Descriptions
{
    internal sealed class TestDescription : IEnumerable<TestDescription.TestRecord>
    {
        public class TestRecord
        {

            public string[] BoundingParameters;

            public ProjectionElement Element;

            public override string ToString()
            {
                return string.Format("{0}",Element.Name);
            }

            public TestRecord(ProjectionElement element,string[] orderedBoundingParameters)
            {
                Element = element;
                BoundingParameters = orderedBoundingParameters;
            }
        }

        private readonly List<TestRecord>  _tests = new List<TestRecord>();
        public IEnumerator<TestRecord> GetEnumerator()
        {
            return _tests.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private Expression<Func<FrameNodeGroup, bool>> MakeTestPredicateForElement(TestRecord element)
        {
            return k => k.MatchedElement == element.Element;
        }

        public void Add(TestRecord record)
        {
            _tests.Add(record);
        }

        public bool ExclusiveCondition;

        public Expression<Predicate<FrameNodeGroup[]>> MakeTestPredicateForElements()
        {
            var groups = Expression.Parameter(typeof(FrameNodeGroup[]));

            Func<TestRecord, Expression<Func<FrameNodeGroup[], bool>>> builder =
                s =>
                    {
                        var predicate = MakeTestPredicateForElement(s).Compile();
                        return (k => k.Any(predicate));
                    };

            var body = _tests.Aggregate(
                ExclusiveCondition
                  ?Expression.Equal(
                     Expression.ArrayLength(groups),
                     Expression.Constant(_tests.Count()))
                  :(Expression)Expression.Constant(true),
                (a, s) =>
                    Expression.AndAlso(a, Expression.Invoke(builder(s), groups))

                    );
            Contract.Assume(body !=null);
            var test = Expression.Lambda<Predicate<FrameNodeGroup[]>>(body, groups);
            return test;
        }

       

        public override string ToString()
        {
            return string.Join("\n",_tests.Select(k=>k.ToString()));
        }
    }
}
