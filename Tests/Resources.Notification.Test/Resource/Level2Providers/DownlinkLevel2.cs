using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;

namespace Resources.Notification.Test.Resource.Level2Providers
{
    [Export(typeof(AnnouncerDownlink))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class DownlinkLevel2 : AnnouncerDownlink
    {
#pragma warning disable 0649
        [Import]
        private LinkToTransporter _transporter;
#pragma warning restore 0649
        private class Observer : IObserver<ResourceDescription>
        {
            TransporterToLevel2 _transporter;

            public Observer(TransporterToLevel2 transporter)
            {
                _transporter = transporter;
            }

            public void OnNext(ResourceDescription value)
            {
                _transporter.MarkResource(value);
            }

            public void OnError(Exception error)
            {

            }

            public void OnCompleted()
            {

            }
        }
      
        private class Token : IDisposable
        {
            private IObserver<ResourceDescription> _obs;
            private DownlinkLevel2 _events;
            public Token(DownlinkLevel2 events, IObserver<ResourceDescription> obs)
            {
                _obs = obs;
                _events = events;
            }

            public void Dispose()
            {
                _events.Unsubscribe(_obs);
            }
        }


        private class ProxyObserver : MarshalByRefObject, IObserver<ResourceDescription>
        {
            private IObserver<ResourceDescription> _observer;

            public ProxyObserver(IObserver<ResourceDescription> observer)
            {
                _observer = observer;
            }

            void IObserver<ResourceDescription>.OnCompleted()
            {
                _observer.OnCompleted();
            }

            void IObserver<ResourceDescription>.OnError(Exception error)
            {
                _observer.OnError(error);
            }

            void IObserver<ResourceDescription>.OnNext(ResourceDescription value)
            {
                _observer.OnNext(value);
            }
        }

        public override IDisposable Subscribe(IObserver<ResourceDescription> observer)
        {
            _transporter.Transporter.SetObserver(new ProxyObserver(observer));
           
            return new Token(this, observer);
        }


        private void Unsubscribe(IObserver<ResourceDescription> observer)
        {
            _transporter.Transporter.SetObserver(null);
        }


    }



}
