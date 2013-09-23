using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Utility.Dictionary
{


    public sealed unsafe class GuidQuickComparer : Comparer<Guid>,IEqualityComparer<Guid>
    {

        private readonly long[] _key;

        private readonly long[] _partKeys;

        public GuidQuickComparer(long[] key)
        {
            _key = key;
            _partKeys = new long[key.Length];
            long s = 0;
            for (int i = 0; i < key.Length; i++)
            {
                s |= _key[i];
                _partKeys[i] = s;
            }
        }



        public override int Compare(Guid id1, Guid id2)
        {

            long i1 = *((long*) &id1);
            long i2 = *((long*) &id2);

            var eq = (i1 ^ i2);
            if (eq == 0)
                return 0;

            var i1more = i1 & eq;
            var i2more = i2 & eq;
            if (i1more == 0)
                return -1;
            if (i2more == 0)
                return 1;
            int maxI1;

            for (maxI1 = 0; (_key[maxI1] & i1more) == 0; maxI1++) ;

            if ((_partKeys[maxI1] & i2more) == 0)
                return 1;
            return -1;

        }

        public bool Equals(Guid x, Guid y)
        {
            return EqualityComparer<Guid>.Default.Equals(x,y);
        }

        public int  GetHashCode(Guid obj)
        {
            return EqualityComparer<Guid>.Default.GetHashCode(obj);
        }

      
    }
}
