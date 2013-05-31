using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Dataspace.Common.Announcements;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resources.Notification.Test.Resource.Level1Providers;
using Resources.Test.TestResources;
using Rhino.Mocks;

namespace Resources.Notification.Test.UnitTests
{
    [TestClass]
    public class TransactionsTest
    {
        private CompositionContainer _container;
        private MockRepository _mockRepository = new MockRepository();
        private NotifiedElementPoster _poster;
        private IPacketResourcePoster _posterFactory;
        public TestContext TestContext { get; set; }

        private ITypedPool _pool;

        [TestInitialize]
        public void Initialize()
        {
            _container = new CompositionContainer(
                new AggregateCatalog(
                    new AssemblyCatalog(
                        typeof (ITypedPool).Assembly
                        ),
                    new TypeCatalog(typeof (Registrator)),
                    new TypeCatalog(typeof (ResourcePool)),
                    new TypeCatalog(typeof (NotifiedElementGetter)),
                    new TypeCatalog(typeof (UnnotifiedElementGetter)),
                    new TypeCatalog(typeof (UnnotifiedElementPoster))));
            _poster = _mockRepository.DynamicMock<NotifiedElementPoster>();            
            _posterFactory = _mockRepository.DynamicMock<IPacketResourcePoster>();
            _container.ComposeExportedValue<ResourcePoster>(_poster);
            _container.ComposeExportedValue(_posterFactory);
            var service = _container.GetExportedValue<ICacheServicing>();
            service.Initialize(new Settings(), _container);
            _pool = _container.GetExportedValue<ITypedPool>();
        }     

        [TestMethod]                
        [TestCategory("Transaction")]
        public void LocalizedWriting()
        {
            var id = Guid.NewGuid();
            var el = new NotifiedElement();
            _mockRepository.ReplayAll();         
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
            {                            
                _pool.Post(id, el);
                _posterFactory.AssertWasNotCalled(k => k.PostResourceBlock(null), k => k.IgnoreArguments());
                _poster.AssertWasNotCalled(k => k.WriteResourceRegardlessofTransaction(id, el),
                                           k => k.IgnoreArguments());
                scope.Complete();
            }
           
            _posterFactory.AssertWasCalled(k => k.PostResourceBlock(null),k=>k.IgnoreArguments()
                    .Repeat.Once());           
        }

        [TestMethod]
        [TestCategory("Transaction")]
        public void WritingMultipleObjects()
        {
            var id = Guid.NewGuid();
            var el = new NotifiedElement{Name = "1"};
            var id2 = Guid.NewGuid();
            var el2 = new NotifiedElement { Name = "2" };
            _mockRepository.ReplayAll();
            object[] args = null;
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                _pool.Post(id, el);
                _pool.Post(id2, el2);
                _posterFactory.AssertWasNotCalled(k => k.PostResourceBlock(null), k => k.IgnoreArguments());
                _poster.AssertWasNotCalled(k => k.WriteResourceRegardlessofTransaction(id, el),
                                           k => k.IgnoreArguments());
                _poster.BackToRecord();
                _posterFactory.Stub(k => k.PostResourceBlock(null)).IgnoreArguments()
                                          .WhenCalled(k => { args = k.Arguments;})
                                          .Return(new DataRecord[0]);
                scope.Complete();
            }
            
            _posterFactory.AssertWasCalled(k => k.PostResourceBlock(null), k2 => k2.IgnoreArguments()
                          .Repeat.Once());
            var resPosted = (args[0] as IEnumerable<DataRecord>).ToArray();
            Assert.AreEqual(id,  resPosted[0].Content.ResourceKey);
            Assert.AreNotSame(el,resPosted[0].Resource);
            Assert.AreEqual(el.Name, (resPosted[0].Resource as NotifiedElement).Name);
            Assert.AreEqual(id2, resPosted[1].Content.ResourceKey);
            Assert.AreNotSame(el2, resPosted[1].Resource);
            Assert.AreEqual(el2.Name, (resPosted[1].Resource as NotifiedElement).Name);
        }


        [TestMethod]        
        [TestCategory("Transaction")]
        public void LocalizedWritingWithInnerScopeWriting()
        {
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                var id = Guid.NewGuid();
                var el = new NotifiedElement();
                _mockRepository.ReplayAll();
                using (var innerScope = new TransactionScope(TransactionScopeOption.Required))
                {                    
                    _pool.Post(id, el);
                    innerScope.Complete();
                }
                _posterFactory.AssertWasNotCalled(k => k.PostResourceBlock(null), k => k.IgnoreArguments());                                                
                scope.Complete();
            }
            _posterFactory.AssertWasCalled(k => k.PostResourceBlock(null), k => k.IgnoreArguments());                                
        }
    }
}
