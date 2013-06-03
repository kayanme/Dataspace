using System;
using System.Collections.Generic;
using System.Linq;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Projections.Services.PlanBuilding;
using Dataspace.Common.ServiceResources;

namespace Dataspace.Common.Projections.Classes
{
    internal class Relation
    {
        public ProjectionElement ParentElement;

        public ProjectionElement ChildElement;

        public IEnumerable<Query> Queries = new Query[0];


        public bool HasTrivialQuery { get { return Queries == null; } }

        private static double CalcQueryRank(Query query, string parentName, BoundingParameter[] parameters)
        {

            Func<string, string, bool> equals =
                (a, b) => string.Equals(a, b, StringComparison.InvariantCultureIgnoreCase);

            //отсев запросов, у которых есть параметры, не являющиеся родительским параметром или одним параметром из списка
            if (query.Arguments
                     .Where(k => !equals(k, parentName))
                     .Any(k => parameters.All(k2 => !equals(k2.Name, k))))
                return -1;

            double rank = 0.1;


            if (query.Arguments.Contains(parentName) && query.ArgCount == 1)
            {
                rank = 0.5;
            }
            else if (parameters.Length != 0)
            {
                var correctiveFloor = parameters.Max(k => k.Depth);
                //нужно перевести глубину параметров от текущей точки в рейтинг для текущей точки, т.е. перевернуть их все
                rank =
                   query.Arguments.Where(k => !equals(k, parentName)).Select(
                       k => correctiveFloor - parameters.Single(k2 => equals(k2.Name, k)).Depth + 1).Sum();
            }

            if (query.Arguments.Contains(parentName))
            {


                rank *= 2;

                if (query.SerialQueryIsPreferred(parentName))
                    rank += 1;
            }

            if (!string.IsNullOrEmpty(query.Namespace))
                rank *= 100;
            return rank;
        }



        public IEnumerable<Query> SelectTheBestQuery(BoundingParameter[] parameters)
        {            
            var paramsWithoutResource = parameters;
            var rankedQueries = Queries.Select(k => new { rank = CalcQueryRank(k, ParentElement.Name, paramsWithoutResource), query = k }).ToArray();
            var bestQuery = rankedQueries.Where(k => k.rank >= 0)
                                         .OrderByDescending(k => k.rank)
                                         .Select(k => k.query);
            return bestQuery;
        }
    }
}
