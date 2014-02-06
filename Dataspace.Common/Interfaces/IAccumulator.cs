using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Interfaces
{
    internal interface IAccumulator<in TKey,out TValue>
    {
        Func<TValue> GetValue(TKey id);
    }
}
