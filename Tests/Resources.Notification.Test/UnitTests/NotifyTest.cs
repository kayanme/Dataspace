using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBehave.Narrator.Framework;
using Resources.Notification.Test.Resource.Level1Providers;
using Resources.Test.Providers;
using Resources.Test.TestResources;
using Rhino.Mocks;

namespace Resources.Notification.Test.UnitTests
{
    [TestClass]
    public class NotifyTest
    {
        private CompositionContainer container;
        private MockRepository _mockRepository = new MockRepository();

        public TestContext TestContext { get; set; }

        private ITypedPool _pool;
        private IAnnouncerSubscriptor _subscriptor;
        private AnnouncerUplink _uplink;
        private AnnouncerDownlink _downlink;

        [TestInitialize]
        public void Prepare()
        {
            container = new CompositionContainer(
                new AggregateCatalog(
                    new AssemblyCatalog(
                        typeof (ITypedPool).Assembly
                        ),
                    new TypeCatalog(typeof (Registrator)),
                    new TypeCatalog(typeof(ResourcePool)),
                    new TypeCatalog(typeof (NotifiedElementGetter)),
                    new TypeCatalog(typeof (UnnotifiedElementGetter)),
                    new TypeCatalog(typeof (NotifiedElementPoster)),
                    new TypeCatalog(typeof (UnnotifiedElementPoster))));
            _uplink = _mockRepository.DynamicMock<AnnouncerUplink>();
            _downlink = _mockRepository.DynamicMock<AnnouncerDownlink>(); 
            _downlink.Expect(k => k.Subscribe(null))                   
                   .IgnoreArguments()
                   .Repeat.Once();
            _downlink.Replay();
            
            container.ComposeExportedValue(_uplink);
            container.ComposeExportedValue(_downlink);
            var service = container.GetExportedValue<ICacheServicing>(); 
            service.Initialize(new Settings
                                   {
                                       AutoSubscription = (TestContext.Properties["AutoSubscription"] as string)== "true"                                      
                                   }, container);
            _pool = container.GetExportedValue<ITypedPool>();
            _subscriptor = container.GetExportedValue<IAnnouncerSubscriptor>();                        
        }

        [TestMethod]
        [TestProperty("AutoSubscription","false")]       
        [TestCategory("Notification")]
        public void Subscription()
        {                  
            _uplink.Expect(k=>k.OnNext(null)).IgnoreArguments().Repeat.Never();
            _mockRepository.ReplayAll();
            _pool.Post(Guid.NewGuid(),new UnnotifiedElement());
            _mockRepository.VerifyAll();
        }

        [TestMethod]
        [TestProperty("AutoSubscription", "false")]        
        [TestCategory("Notification")]
        public void Subscription2()
        {            
            _uplink.Expect(k => k.OnNext(null)).IgnoreArguments().Repeat.Once();            
            _mockRepository.ReplayAll();            
            var id = Guid.NewGuid();
            _subscriptor.SubscribeForResourceChange("NotifiedElement",id);          
            _pool.Post(id, new NotifiedElement());
            _mockRepository.VerifyAll();
        }


        [TestMethod]
        [TestProperty("AutoSubscription", "false")]        
        [TestCategory("Notification")]
        public void SubscriptionCheck()
        {
            _subscriptor.SubscribeForResourceChange<NotifiedElement>();
            Assert.IsTrue(container.GetExportedValue<NotifiedElementGetter>().IsTracking); 
        }

        [TestMethod]
        [TestProperty("AutoSubscription", "false")]        
        [TestCategory("Notification")]
        public void SubscriptionCheck2()
        {                      
            Assert.IsFalse(container.GetExportedValue<NotifiedElementGetter>().IsTracking);
        }

        

        [TestMethod]
        [TestProperty("AutoSubscription", "true")]       
        [TestCategory("Notification")]
        public void SubscriptionCheck3()
        {            
            Assert.IsTrue(container.GetExportedValue<NotifiedElementGetter>().IsTracking);
        }    

        [TestMethod]
        [TestCategory("Transaction")]
        [TestCategory("Notification")]
        public void NotifyFeatures()
        {
            "UnitTests\\TransactionNotify.feature".ExecuteFile();
        }
    }
}

