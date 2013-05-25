using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Projections.Services.PlanBuilding;
using Dataspace.Common.ServiceResources;

namespace Dataspace.Common.Projections.Classes.Utilities
{
    internal static class NetSolving
    {

        private static StringComparer _comparer = StringComparer.Create(CultureInfo.InvariantCulture, true);

        //private static bool RelationHasMultipleGetter(this Relation relation, string[] parameters)
        //{
        //    var seriaWithParameters = relation.SeriaQueries.SelectQueryWithMatchingParams(parameters);
        //    var singleWithParameters = relation.Queries.SelectQueryWithMatchingParams(parameters);
        //    var physSeriaWithParameters = relation.SeriaQueriesFromPhysicalSpace.SelectQueryWithMatchingParams(parameters);
        //    var physSingleWithParameters =  relation.SeriaQueriesFromPhysicalSpace.SelectQueryWithMatchingParams(parameters);


        //    var seriaWithNoParameters = relation.SeriaQueries.SelectQueryWithMatchingParams(new string[0]);
        //    var singleWithNoParameters = relation.Queries.SelectQueryWithMatchingParams(new string[0]);
        //    var physSeriaWithNoParameters =
        //        relation.SeriaQueriesFromPhysicalSpace.SelectQueryWithMatchingParams(new string[0]);
        //    var physSingleWithNoParameters =
        //        relation.SeriaQueriesFromPhysicalSpace.SelectQueryWithMatchingParams(new string[0]);

        //    bool hasMultiple;

        //    if (seriaWithParameters.Any())
        //    {
        //        hasMultiple = true;
        //    }
        //    else if (singleWithParameters.Any())
        //    {
        //        hasMultiple = false;
        //    }
        //    else if (physSeriaWithParameters.Any())
        //    {
        //        hasMultiple = true;
        //    }
        //    else if (physSingleWithParameters.Any())
        //    {
        //        hasMultiple = false;
        //    }
        //    else if (!parameters.Any())
        //    {
        //        hasMultiple = false;
        //    }
        //    else if (seriaWithNoParameters.Any())
        //    {
        //        hasMultiple = true;
        //    }
        //    else if (singleWithNoParameters.Any())
        //    {
        //        hasMultiple = false;
        //    }
        //    else if (physSeriaWithNoParameters.Any())
        //    {
        //        hasMultiple = true;
        //    }
        //    else if (physSingleWithNoParameters.Any())
        //    {
        //        hasMultiple = false;
        //    }
        //    else
        //    {
        //        hasMultiple = false;
        //    }

        //    return hasMultiple;
        //}

        //public static int MultipleGetterCount(this ProjectionElement target, string[] parameters)
        //{
        //    var count = target.DownRelations.Count(k => k.RelationHasMultipleGetter(parameters));
        //    return count;
        //}

        private static int? AccessPathToLength(Stack<ProjectionElement> path, ProjectionElement target, ProjectionElement source, int depth)
        {
            if (source == target)
                return 0;
            if (path.Contains(source) || depth == 0)
                return null;
            path.Push(source);
            int? length;
            var paths =
                source.DownRelations.Select(k => k.ChildElement)
                    .Select(k => AccessPathToLength(path, target, k, depth - 1))
                    .Where(k => k.HasValue)
                    .Select(k => k.Value)
                    .ToArray();
            if (paths.Any())
                length = paths.Min();
            else
            {
                length = null;
            }
            path.Pop();
            return length;
        }

        public static int? AccessPathToLength(this ProjectionElement target, ProjectionElement source, int depth)
        {
            return AccessPathToLength(new Stack<ProjectionElement>(), target, source, depth);
        }

        public static bool HasAccessibilty(this ProjectionElement target, IEnumerable<ProjectionElement> sources, int depth)
        {
            return sources.Any(k => AccessPathToLength(target, k, depth).HasValue);
        }


        public static IEnumerable<Query> SelectQueryWithMatchingParams(this IEnumerable<Query> queries, string[] parameters)
        {
           return queries.Where(k =>k.ArgCount == parameters.Length  
                                 && parameters.All(k2 => k.Arguments.Any(k3 =>_comparer.Equals(k3,k2))));
        }


    }
}
