using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Utility.Dictionary
{
    public sealed class GuidQuickComparer : IEqualityComparer<Guid>,IComparer<Guid>
    {
        public bool Equals(Guid x, Guid y)
        {
            unsafe
            {
                var a = (long*)&x;
                var b = (long*)&y;
                return a[0] == b[0] && a[1] == b[1];
            }
        }

        public int GetHashCode(Guid obj)
        {
            unsafe
            {
                unchecked
                {
                    var a = (int*)&obj;

                    return a[0] + a[1] + a[2] + a[3];
                }
            }
        }

        public int Compare(Guid x, Guid y)
        {
          
            unsafe
            {
                var a = (ulong*)&x;
                var b = (ulong*)&y;
                return a[0] != b[0] ? (a[0] > b[0] ? -1 : 1) : (a[1] > b[1] ? -1 : (a[1] < b[1] ? 1 : 0));
            }
        }
    }
}
