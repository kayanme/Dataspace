using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Security;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Resources.Security.Test.SecurityResources;
using Resources.Test.TestResources;
using Resources.Test.TestResources.SecurityResource;
using Testhelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Resources.Security.Test
{
    [TestClass]
    public class SecurityTest
    {
        private Element _singleElement;
        private Model _singleModel;
        private Element _dependentElement;
        private Model _dependentModel;

     

        protected Assembly[] AssembliesWhichShouldProvideExport
        {
            get { return new[] { typeof(Model).Assembly, typeof(ITypedPool).Assembly }; }
        }

        private ITypedPool _cachier;

        private ITypedPool GetCachier()
        {
            return _cachier;
        }

        [TestMethod]
        [TestCategory("Security")]
        [TestCategory("Caching")]
        public void BasicSecurity()
        {
            var cachier = GetCachier();

            var modelReader = Guid.NewGuid();
            var modelWriter = Guid.NewGuid();
            var elementReader = Guid.NewGuid();
            var elementWriter = Guid.NewGuid();

            var elemPermissions = new SecurityPermissions
                                      {
                                          AllowedForRead = new[] {elementReader},
                                          AllowedForWrite = new[] {elementWriter}
                                      };

            var modelPermissions = new SecurityPermissions
                                       {
                                           AllowedForRead = new[] {modelReader},
                                           AllowedForWrite = new[] {modelWriter}
                                       };

            cachier.Post(_singleModel.Key, modelPermissions);
            cachier.Post(_dependentModel.Key, modelPermissions);
            cachier.Post(_singleElement.Key, elemPermissions);
            cachier.Post(_dependentElement.Key, elemPermissions);

            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            var group1 = new SecurityGroup {Groups = new[] {modelReader, elementReader, elementWriter}};

            var group2 = new SecurityGroup {Groups = new[] {modelReader, elementReader, modelWriter}};

            cachier.Post(user1, group1);
            cachier.Post(user2, group2);

            Session.UserId = user1;
            var elem = cachier.Get<Element>(_singleElement.Key);
            Assert.IsNotNull(elem);
            var model = cachier.Get<Model>(_singleModel.Key);
            Assert.IsNotNull(model);
            ModelSecurityProvider.TimesWasUsed = 0;//после этого должы браться закэшированные результаты
            try
            {
                cachier.Post(_singleModel.Key, _singleModel);
                Assert.Fail();
            }
            catch (SecurityException)
            {

            }

            cachier.Post(_singleElement.Key, _singleElement);
            Assert.AreEqual(0,ModelSecurityProvider.TimesWasUsed);

            Session.UserId = user2;
            elem = cachier.Get<Element>(_singleElement.Key);
            Assert.IsNotNull(elem);
            model = cachier.Get<Model>(_singleModel.Key);
            Assert.IsNotNull(model);
            Assert.AreEqual(2,ModelSecurityProvider.TimesWasUsed);//поскольку берем в другом контексте безопасности, кэшироваться еще ничего было не должно
            ModelSecurityProvider.TimesWasUsed = 0;//после этого снова должны браться закэшированные результаты
            try
            {
                cachier.Post(_singleElement.Key, _singleElement);
                Assert.Fail();
            }
            catch (SecurityException)
            {

            }
            cachier.Post(_singleModel.Key, _singleModel);
            Assert.AreEqual(0,ModelSecurityProvider.TimesWasUsed);
         
        }

        [TestMethod]
        [TestCategory("Security")]              
        public void ChangingSecurity()
        {
            var cachier = GetCachier();

            var modelReader = Guid.NewGuid();
            var modelWriter = Guid.NewGuid();
            var elementReader = Guid.NewGuid();
            var elementWriter = Guid.NewGuid();

            var elemPermissions = new SecurityPermissions
            {
                AllowedForRead = new[] { elementReader },
                AllowedForWrite = new[] { elementWriter }
            };

            var modelPermissions = new SecurityPermissions
            {
                AllowedForRead = new[] { modelReader },
                AllowedForWrite = new[] { modelWriter }
            };

            cachier.Post(_singleModel.Key, modelPermissions);
            cachier.Post(_dependentModel.Key, modelPermissions);
            cachier.Post(_singleElement.Key, elemPermissions);
            cachier.Post(_dependentElement.Key, elemPermissions);

            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            var group1 = new SecurityGroup { Groups = new[] { modelReader, elementReader, elementWriter } };

            var group2 = new SecurityGroup { Groups = new[] { modelReader, elementReader, modelWriter } };

            cachier.Post(user1, group1);
            cachier.Post(user2, group2);

            Session.UserId = user1;
            var elem = cachier.Get<Element>(_singleElement.Key);
            Assert.IsNotNull(elem);
            var model = cachier.Get<Model>(_singleModel.Key);
            Assert.IsNotNull(model);

            //запрещаем чтение из модели
            group1.Groups = new[] {elementReader, elementWriter};

            elem = cachier.Get<Element>(_singleElement.Key);
            Assert.IsNotNull(elem);
            model = cachier.Get<Model>(_singleModel.Key);
            Assert.IsNotNull(model);

            cachier.Post(user1, group1);
            elem = cachier.Get<Element>(_singleElement.Key);
            Assert.IsNotNull(elem);
            model = cachier.Get<Model>(_singleModel.Key);
            Assert.IsNull(model);
            MockHelper.AwaitException<SecurityException>(() => cachier.Post(_singleModel.Key, _singleModel));           
            cachier.Post(_singleElement.Key, _singleElement);

            //разрешаем только запись во все
            group1.Groups = new[] { modelWriter, elementWriter };
            cachier.Post(user1, group1);
            cachier.Post(_singleModel.Key, _singleModel);
            cachier.Post(_singleElement.Key, _singleElement);

            Session.UserId = user2;
            elem = cachier.Get<Element>(_singleElement.Key);
            Assert.IsNotNull(elem);
            model = cachier.Get<Model>(_singleModel.Key);
            Assert.IsNotNull(model);
            MockHelper.AwaitException<SecurityException>(() => cachier.Post(_singleElement.Key, _singleElement));          
            cachier.Post(_singleModel.Key, _singleModel);
        }


        [TestMethod]
        [TestCategory("Security")]
        [TestCategory("Caching")]
        public void DeferredSecurity()
        {

            var cachier = GetCachier();
            ModelSecurityProvider.TimesWasUsed = 0;
            var modelReader = Guid.NewGuid();
            var modelWriter = Guid.NewGuid();
            var elementReader = Guid.NewGuid();
            var elementWriter = Guid.NewGuid();

            var elemPermissions = new SecurityPermissions
            {
                AllowedForRead = new[] { elementReader },
                AllowedForWrite = new[] { elementWriter }
            };

            var modelPermissions = new SecurityPermissions
            {
                AllowedForRead = new[] { modelReader },
                AllowedForWrite = new[] { modelWriter }
            };

            cachier.Post(_singleModel.Key, modelPermissions);
            cachier.Post(_dependentModel.Key, modelPermissions);
            cachier.Post(_singleElement.Key, elemPermissions);
            cachier.Post(_dependentElement.Key, elemPermissions);

            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            var group1 = new SecurityGroup { Groups = new[] { modelReader, elementReader, elementWriter } };

            var group2 = new SecurityGroup { Groups = new[] { modelReader, elementReader, modelWriter } };

            cachier.Post(user1, group1);
            cachier.Post(user2, group2);

            Session.UserId = user1;


            var elem = cachier.GetLater<Element>(_singleElement.Key);           
            var elem2 = cachier.GetLater<Element>(_dependentElement.Key);
            
            Assert.IsNotNull(elem.Value);
            Assert.IsNotNull(elem2.Value);
            Assert.AreEqual(1, ModelSecurityProvider.TimesWasUsed);//серия должна взяться один раз
           //после этого должы браться результаты серии закэшированные результаты
            ModelSecurityProvider.TimesWasUsed = 0;
            cachier.Post(_singleElement.Key, _singleElement);
            cachier.Post(_dependentElement.Key, _dependentElement);
            Assert.AreEqual(0, ModelSecurityProvider.TimesWasUsed);

            Session.UserId = user2;
            elem = cachier.GetLater<Element>(_singleElement.Key);
            elem2 = cachier.GetLater<Element>(_dependentElement.Key);

            Assert.IsNotNull(elem.Value);
            Assert.IsNotNull(elem2.Value);
            Assert.AreEqual(1, ModelSecurityProvider.TimesWasUsed);//серия должна взяться один раз
            //после этого должы браться результаты серии закэшированные результаты
            ModelSecurityProvider.TimesWasUsed = 0;
            MockHelper.AwaitException<SecurityException>(() => cachier.Post(_singleElement.Key, _singleElement));
            MockHelper.AwaitException<SecurityException>(() =>cachier.Post(_dependentElement.Key, _dependentElement));

            Assert.AreEqual(0, ModelSecurityProvider.TimesWasUsed);

        }


        [TestInitialize]
        public void Preparation()
        {
            var container =
                new CompositionContainer(new AggregateCatalog(AssembliesWhichShouldProvideExport.Select(k=>new AssemblyCatalog(k))));
                

            _singleModel = new Model { Element = Guid.Empty, Key = Guid.NewGuid(), Name = "SingleModel" };
            _singleElement = new Element() { Model = Guid.Empty, Key = Guid.NewGuid(), Name = "SingleElement" };
            _dependentModel = new Model { Element = Guid.NewGuid(), Key = Guid.NewGuid(), Name = "DepModel" };
            _dependentElement = new Element() { Model = _dependentModel.Key, Key = _dependentModel.Element, Name = "DepElement" };
            var pool = new ResourcePool();
            pool.Models.Add(_singleModel.Key, _singleModel);
            pool.Models.Add(_dependentModel.Key, _dependentModel);
            pool.Elements.Add(_singleElement.Key, _singleElement);
            pool.Elements.Add(_dependentElement.Key, _dependentElement);
            
            container.ComposeExportedValue(pool);
            container.ComposeExportedValue(container);
            Settings.NoCacheGarbageChecking = true;
            _cachier = container.GetExportedValue<ITypedPool>();         
        }      
    }
}
