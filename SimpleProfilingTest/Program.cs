using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Resources.Test;

namespace SimpleProfilingTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new CachingDictionaryTest();
            test.ParallelCachingDictionaryTests();        
        }
    }
}
