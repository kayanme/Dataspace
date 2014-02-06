using System;
using System.Collections.Generic;
using Dataspace.Common.Announcements;
using Dataspace.Common.Data;

namespace Dataspace.Common.Interfaces.Internal
{
    internal interface IInterchangeCachier : IObserver<ResourceDescription>
    {
        void Push<T>(Guid key, T resource);
        void MarkSubscriptionForResource(Type t);
        void UnmarkSubscriptionForResource(Type t);
        void MarkForUpdate(UnactualResourceContent res);
        void CachePanic();
        void LockCaches(IEnumerable<string> resNames, List<string> alreadyProcessed);
        void UnlockCaches(IEnumerable<string> resNames, List<string> alreadyProcessed);
    }

}
