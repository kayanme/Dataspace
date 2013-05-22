using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Testhelper
{
    public static class MockHelper
    {         

        public static AggregateCatalog CatalogForContainer(IEnumerable<Assembly> assemblies, IEnumerable<Type> types)
        {
           return new AggregateCatalog(
               assemblies
                   .Select(k => new AssemblyCatalog(k) as ComposablePartCatalog)                  
                   .Concat(types.Select(k => new TypeCatalog(k)))
                   .Concat(new[] { new TypeCatalog(typeof(AggregateCatalog)) }));
        }

        public static T MeasureOperation<T>(Func<T> operation, out TimeSpan time)
        {
            var watch = Stopwatch.StartNew();
            var res = operation();
            watch.Stop();
            time = watch.Elapsed;
            return res;
        }

        public static void MeasureOperation(Action operation, out TimeSpan time)
        {
            var watch = Stopwatch.StartNew();
            operation();
            watch.Stop();
            time = watch.Elapsed;
        }

        public static CompositionContainer InitializeContainer(IEnumerable<Assembly> assemblies, IEnumerable<Type> types)
        {
            var aggr = CatalogForContainer(assemblies, types);          
            var container = new CompositionContainer(aggr);                    
            return container;
        }


        public static IEnumerable<long> GetNumericRandomSequence(int count, int upperBound)
        {
            Func<Random, long> gen = g => ((long)g.Next(upperBound));
            return count.Times().Select(k => gen(rnd)).ToArray();
        }

        private static T Random<T>(Random g)
        {
            if (typeof(T) == typeof(string))
                return (T)(object)(g.Next(7).Times().Aggregate("", (a, s) => a + (char)(g.Next(25) + 65)));

            if (typeof(T) == typeof(long))
                return (T)(object)((long)g.Next(5000));

            if (typeof(T) == typeof(int))
                return (T)(object)(g.Next(5000));

            if (typeof(T) == typeof(double))
                return (T)(object)(g.NextDouble());
            if (typeof(T) == typeof(float))
                return (T)(object)(float)(g.NextDouble());

            if (typeof(T) == typeof(DateTime))
                return (T)(object)(new DateTime(g.Next(3) + 2007, g.Next(12) + 1, g.Next(27) + 1, g.Next(24), g.Next(60), g.Next(60)));

            if (typeof(T) == typeof(bool))
                return (T)(object)(g.Next(2) == 1);

            if (typeof(T) == typeof(Guid))
                return (T)(object)Guid.NewGuid();

            throw new ArgumentException();
        }


        public static IEnumerable<Tuple<T, K>> RandomPairs<T, K>(int count)
        {
            return count.Times().Select(k => Tuple.Create(Random<T>(rnd), Random<K>(rnd))).ToArray();
        }


        private static readonly Random rnd = new Random();

       
        public static void AwaitException<T>(Action a) where T:Exception
        {
            try
            {
                a();
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Exception "+typeof(T).Name+" expected.");
            }
            catch (T)
            {
            }
        }
      
        public static IEnumerable<object> GetRandomSequence(int count)
        {

            Func<Random, int, object> gen = (g, t) =>
            {
                if (t == 0)
                    return (g.Next(7).Times().Aggregate("", (a, s) => a + (char)(g.Next(25) + 65)));

                if (t == 1)
                    return ((long)g.Next(5000));

                if (t == 2)
                    return (g.NextDouble());

                if (t == 3)
                    return (new DateTime(g.Next(3) + 2007, g.Next(12) + 1, g.Next(27) + 1, g.Next(24), g.Next(60), g.Next(60)));

                if (t == 4)
                    return (g.Next(2) == 1);

                if (t == 5)
                    return Guid.NewGuid();

                throw new ArgumentException();
            };


            return count.Times().Select(k => gen(rnd, rnd.Next(6))).ToArray();
        }
       
        private static IEnumerable<int> Times(this int count)
        {

            for (var i = 0; i < count; i++)
                yield return i;
        }

        public static IEnumerable<T> GetRandomSequence<T>(int count)
        {
            for (var i = 0; i < count; i++)
                yield return Random<T>(rnd);
        }

        public static void CheckSequences<T>(IEnumerable<T> act, IEnumerable<T> exp)
        {
            var lact = act.ToArray();
            var lexp = exp.ToArray();
            var matches = lact.Zip(lexp, (a, e) => new { a, e });
            try
            {
                if (lact.Count() != lexp.Count())
                    throw new AssertFailedException();
                if (matches.Any(k => !Equals(k.a, k.e)))
                    throw new AssertFailedException();
            }
            catch (AssertFailedException)
            {
                var parts =
                    matches.Select(k => string.Format(Equals(k.a, k.e) ? "{0} - {1} " : "--->  {0} - {1}", k.a, k.e));

                Assert.Fail("Actual - expected \n" + string.Join("\n", parts));
            }
        }

       

    }
}
