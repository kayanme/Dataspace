using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Dataspace.Common.Utility
{
    internal class IndexedCollection<T> : IEnumerable<T> 
    {

        internal Dictionary<Expression<Func<T, object>>,
            ConcurrentDictionary<object, IEnumerable<T>>> Indexes =
                new Dictionary<Expression<Func<T, object>>,
                    ConcurrentDictionary<object, IEnumerable<T>>>(new ExpressionTreeComparer());


        private List<T> _allElements = new List<T>();


        public IndexedCollection(params Expression<Func<T, object>>[] indexes)
        {

            foreach (var expression in indexes)
            {
                var index = new ConcurrentDictionary<object, IEnumerable<T>>();
                Indexes.Add(expression, index);

            }
        }

        public long Count
        {
            get { return _allElements.Count; }
        }

        public void Add(T obj)
        {
            foreach (var index in Indexes)
            {
                var value = index.Key.Compile()(obj);
                if (value != null)
                {
                    var list = index.Value.GetOrAdd(value, new List<T>());
                    (list as List<T>).Add(obj);
                }
            }
            _allElements.Add(obj);
        }


        public void AddRange(IEnumerable<T> objs)
        {
            foreach (var obj in objs)
            {
                Add(obj);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _allElements.GetEnumerator();
        }



        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }

    namespace Linq
    {
        internal static class Queries
        {
            [ThreadStatic]
            internal static int TimesWasHere = 0;

            [ThreadStatic]
            internal static bool Debug = false;

            private static void CheckForEqualExpression<T>(
                IndexedCollection<T> source,
                List<IEnumerable<T>> concatenatedParts,
                Expression activePart,
                ref Expression outRawExpression,
                ParameterExpression parameter)
            {
                if (activePart.NodeType == ExpressionType.Equal)
                {                
                  
                    var leftRawExpression = (activePart as BinaryExpression).Left;
                    var leftExpression = Expression.Lambda<Func<T, object>>(Expression.Convert(leftRawExpression, typeof(object)), parameter);

                    var rightRawExpression = (activePart as BinaryExpression).Right;
                    var rightExpression = Expression.Lambda<Func<T, object>>(Expression.Convert(rightRawExpression, typeof(object)), parameter);

                    if (source.Indexes.ContainsKey(leftExpression))
                    {
                    if (Debug)
                        TimesWasHere++;

                        concatenatedParts.Add(source.Indexes[leftExpression][rightExpression.Compile()(default(T))]);
                        return;
                    }
                    if (source.Indexes.ContainsKey(rightExpression))
                    {
                    if (Debug)
                        TimesWasHere++;

                        concatenatedParts.Add(source.Indexes[rightExpression][leftExpression.Compile()(default(T))]);
                        return;
                    }
                }

                if (outRawExpression == null)
                    outRawExpression = activePart;
                else
                {
                    outRawExpression = Expression.And(activePart, outRawExpression);
                }

            }

            public static bool Any<T>(this IndexedCollection<T> source, Expression<Func<T, bool>> query)
            {
                return Equals(source.Where(query).FirstOrDefault(), default(T));
            }

            public static bool All<T>(this IndexedCollection<T> source, Expression<Func<T, bool>> query) 
            {
                return source.Where(query).Count() == source.Count;
            }

            public static T First<T>(this IndexedCollection<T> source, Expression<Func<T, bool>> query) 
            {
                return source.Where(query).First();
            }

            public static T FirstOrDefault<T>(this IndexedCollection<T> source, Expression<Func<T, bool>> query) 
            {
                return source.Where(query).FirstOrDefault();
            }

            public static IEnumerable<T> Where<T>(this IndexedCollection<T> source, Expression<Func<T, bool>> query) 
            {
                Contract.Requires(source != null);
                Contract.Requires(query!=null);
                Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);
                var concatenatedParts = new List<IEnumerable<T>>();
                query = query.Reduce() as Expression<Func<T, bool>> ;
                var parameter = query.Parameters.First();
                Expression outRawExpression = null;

                Expression currentPart;

                for (currentPart = query.Body; currentPart.NodeType == ExpressionType.And || currentPart.NodeType == ExpressionType.AndAlso; currentPart = (currentPart as BinaryExpression).Right)
                {
                    var activePart = (currentPart as BinaryExpression).Left;
                    CheckForEqualExpression(source, concatenatedParts, activePart, ref outRawExpression, parameter);
                }


                if (currentPart.NodeType == ExpressionType.Equal)
                {
                    CheckForEqualExpression(source, concatenatedParts, currentPart, ref outRawExpression, parameter);
                }

                if (!concatenatedParts.Any())
                    return source.AsEnumerable().Where(query.Compile());


                var aggregatedObjects = concatenatedParts.Aggregate((a, s) => a == null ? s : a.Where(s.Contains));


                if (!aggregatedObjects.Any() || outRawExpression == null)
                    return aggregatedObjects;

                var outExpression = Expression.Lambda<Func<T, bool>>(outRawExpression, parameter);
                return aggregatedObjects.Where(outExpression.Compile());
            }
        }
    }
    }

  

