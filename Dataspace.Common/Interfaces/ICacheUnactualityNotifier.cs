using System;
using Dataspace.Common.Announcements;

namespace Dataspace.Common.Interfaces
{
    internal interface ICacheUnactualityNotifier
    {
        void SubscribeForResourceChange(SubscriptionToken token);     
        void UnsubscribeForResourceChange(SubscriptionToken token);      
        void AnnonunceUnactuality(string resourceName, Guid id);
    }
}
