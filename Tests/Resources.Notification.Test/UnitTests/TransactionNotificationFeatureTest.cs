using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBehave.Narrator.Framework;
using Resources.Notification.Test.Resource.Level1Providers;
using Resources.Test.TestResources;
using Rhino.Mocks;

namespace Resources.Notification.Test.UnitTests
{
    [ActionSteps]
    public class TransactionNotificationFeatureTest
    {

        private CompositionContainer container;
        private MockRepository _mockRepository = new MockRepository();
        private NotifiedElementPoster _poster;
        private IPacketResourcePoster _posterFactory;
        private IAnnouncerSubscriptor _subscriptor;        
        private AnnouncerUplink _uplink;
        private AnnouncerDownlink _downlink;
        private ITypedPool _pool;

        private class TransactionPropagater:MarshalByRefObject
        {
            private class TT:IEnlistmentNotification
            {
                public void Prepare(PreparingEnlistment preparingEnlistment)
                {
                    preparingEnlistment.Prepared();
                }

                public void Commit(Enlistment enlistment)
                {
                    enlistment.Done();
                }

                public void Rollback(Enlistment enlistment)
                {
                    enlistment.Done();
                }

                public void InDoubt(Enlistment enlistment)
                {
                    enlistment.Done();
                }
            }

            public void Trans(Transaction tr)
            {               
            
            }
        }

        [BeforeScenario]
        public void Initialize()
        {
            container = new CompositionContainer(
                new AggregateCatalog(
                    new AssemblyCatalog(
                        typeof (ITypedPool).Assembly
                        ),
                    new TypeCatalog(typeof (Registrator)),
                    new TypeCatalog(typeof (ResourcePool)),
                    new TypeCatalog(typeof (NotifiedElementGetter)),
                    new TypeCatalog(typeof(NotifiedElementPoster)),
                    new TypeCatalog(typeof (UnnotifiedElementGetter)),
                    new TypeCatalog(typeof (UnnotifiedElementPoster))));
            _uplink = _mockRepository.DynamicMock<AnnouncerUplink>();
            _downlink = _mockRepository.DynamicMock<AnnouncerDownlink>();
            _downlink.Expect(k => k.Subscribe(null))
                   .IgnoreArguments()
                   .Repeat.Once();
            _downlink.Replay();

            container.ComposeExportedValue(_uplink);
            container.ComposeExportedValue(_downlink);
            //_poster = _mockRepository.DynamicMock<NotifiedElementPoster>();
            //_posterFactory = _mockRepository.DynamicMock<IPacketResourcePoster>();
        //    container.ComposeExportedValue<ResourcePoster>(_poster);
            container.ComposeExportedValue(_posterFactory);
            _pool = container.GetExportedValue<ITypedPool>();
            _subscriptor = container.GetExportedValue<IAnnouncerSubscriptor>();
            var service = container.GetExportedValue<ICacheServicing>();
            service.Initialize(new Settings
            {
                
            }, container);
        }

        

        private TransactionScope _scope;

        [Given("$level isolation level")]
        public void IsolationLevel(IsolationLevel level)
        {
            
            _scope = new TransactionScope(TransactionScopeOption.RequiresNew,
                                          new TransactionOptions {IsolationLevel = level});

        }

        [Given("a $isDistributed transaction")]
        public void DistrubutionMode(string isDistributed)
        {
            if (isDistributed == "yes")
            {
                var d = AppDomain.CreateDomain("Propagation", null, new AppDomainSetup { ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase });
                
                var p = d.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(TransactionPropagater).FullName) as TransactionPropagater;
                p.Trans(Transaction.Current);
            }
        }

        [When("I post a resource")]
        public void PostResource()
        {
                _uplink.Replay();       
                var id = Guid.NewGuid();
                _subscriptor.SubscribeForResourceChange("NotifiedElement", id);
                _pool.Post(id, new NotifiedElement());
        }

        [Then("there $should be an announce after post")]
        public void ShouldAnnounceAfterPost(string should)
        {
            
            if (should == "should")
            {
                _uplink.AssertWasCalled(k => k.OnNext(null),k => k.IgnoreArguments().Repeat.Once());                
            }            
            _uplink.VerifyAllExpectations();
            
        }

        [Then("there $should be an announce after $commit")]
        public void ShouldAnnounceAfterCommitOrRollback(string should,string commit)
        {
            if (commit == "commit")
            {
                _scope.Complete();
            }
               
            _scope.Dispose();
            if (should == "should")
            {
                _uplink.AssertWasCalled(k => k.OnNext(null), k => k.IgnoreArguments().Repeat.Once());
            }
                
            _uplink.VerifyAllExpectations();
        }
    }
}
