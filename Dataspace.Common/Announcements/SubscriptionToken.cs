using System;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;

namespace Dataspace.Common.Announcements
{
    /// <summary>
    /// Маркер подписки на обновление
    /// </summary>
    public class SubscriptionToken
    {
        
        internal Type ResourceType;

        internal string ResourceName;

        internal int SubscriptionCounter;

        /// <summary>
        /// Возникает после потери ресурсом актуальности.
        /// </summary>
        public event ResourceUnactualEventHandler ResourceMarkedAsUnactual;

        /// <summary>
        /// Обновление ресурса.
        /// </summary>
        public class ResourceUnactualEventArgs :EventArgs
        {
            /// <summary>
            /// Обновленны ресурс.
            /// </summary>
            /// <value>
            /// Ресурс.
            /// </value>
            public ResourceDescription Resource { get; internal set; }

            internal ResourceUnactualEventArgs()
            {
                
            }
        }

        public delegate void ResourceUnactualEventHandler(object sender, ResourceUnactualEventArgs e);

        internal void InvokeUnactuality(ResourceDescription resource)
        {
            if (ResourceMarkedAsUnactual!=null)
                ResourceMarkedAsUnactual(this, new ResourceUnactualEventArgs{Resource = resource});
        }

        private readonly AnnouncerSubscriptor _subscriptor;

        /// <summary>
        /// Объединение с подпиской на другой ресурс.
        /// </summary>
        /// <typeparam name="T">Тип ресурса для подписки на обновление. <seealso cref="IAnnouncerSubscriptor"/></typeparam>
        /// <returns>Маркер обновления</returns>
        public SubscriptionToken AlsoSubscribeTo<T>()
        {
            return _subscriptor.MergeWithSubscriptionForResourceChange<T>(this);
        }

        internal SubscriptionToken(AnnouncerSubscriptor subscriptor)
        {
            _subscriptor = subscriptor;
        }
    }
}
