using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Resources.Notification.Test.Resource.Level1Providers;
using Resources.Test.TestResources;

namespace Resources.Notification.Test.Resource
{
    [Export]
    public class TransporterToLevel2:MarshalByRefObject
    {
#pragma warning disable 0649
        [Import]
        private IAnnouncerSubscriptor _subscriptor;
#pragma warning restore 0649
        internal Level1Activator _activator;



        private IObserver<ResourceDescription> _observer;

        private class Token:MarshalByRefObject,IDisposable
        {
            private TransporterToLevel2 _tr;

            public Token(TransporterToLevel2 tr)
            {
                _tr = tr;
            }


            public void Dispose()
            {
                _tr._observer.OnCompleted();
                _tr._observer = null;
                _tr = null;
            }
        }



        public void MarkResource(ResourceDescription res)
        {
            if (_observer != null)
            {
                _observer.OnNext(res);
                _subscriptor.UnsubscribeForResourceChangePropagation(res.ResourceName, res.ResourceKey);
            }
        }

        public T Get<T>(Guid key,bool subscribe) where T:class
        {
            if (subscribe)
                _subscriptor.UnsubscribeForResourceChangePropagation(typeof(T).Name, key);                                 
            return _activator.Get<T>(key);
        }

        public void Post<T>(Guid key,T resource) where T : class
        {
            _activator.Post(key,resource);
        }

        public void SetObserver(IObserver<ResourceDescription> obs)
        {
            _observer = obs;
        }

        public void Subscribe(string resourceName,Guid id)
        {
            _subscriptor.SubscribeForResourceChangePropagation(resourceName, id);
        }

        public void Unsubscribe(string resourceName, Guid id)
        {
            _subscriptor.UnsubscribeForResourceChangePropagation(resourceName, id);
        }
    }
}
