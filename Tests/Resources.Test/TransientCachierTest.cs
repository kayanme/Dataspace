using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Utility;

using Resources.Test.TestResources;
using Resources.Test.TestResources.ElementDeriver;
using Resources.Test.TestResources.ElementInModel;
using Resources.Test.TestResources.ModelDeriver;
using Testhelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using MockHelper = Testhelper.MockHelper;

namespace Resources.Test
{
    
    
    /// <summary>
    ///This is a test class for ITransientCachierTest and is intended
    ///to contain all ITransientCachierTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TransientCachierTest
    {      

        private Element _singleElement;
        private Model _singleModel;
        private Element _dependentElement;
        private Model _dependentModel;
        private CompositionContainer _container;

        internal virtual ITypedPool CreateITransientCachier()
        {

            var target = _container.GetExportedValue<ITypedPool>();
            return target;
        }

        internal virtual IGenericPool CreateNamedTransientCachier()
        {

            var target = _container.GetExportedValue<IGenericPool>();
            return target;
        }

        /// <summary>
        ///A test for Get
        ///</summary>
        public void GetResourceTestHelper<T>()
            where T : class
        {
            var target = CreateITransientCachier();
            if (typeof(T) == typeof(Model))
            {
                Guid id = _singleModel.Key;
                var expected = _singleModel;
                var actual = target.Get<Model>(id);
                Assert.AreNotSame(expected, actual);
                Assert.AreEqual(expected, actual);

                id = _dependentModel.Key;
                expected = _dependentModel;
                actual = target.Get<Model>(id);
                Assert.AreNotSame(expected, actual);
                Assert.AreEqual(expected, actual);

                id = Guid.NewGuid();
                expected = null;
                actual = target.Get<Model>(id);                
                Assert.AreEqual(expected, actual);
            }

            if (typeof(T) == typeof(Element))
            {
                var id = _singleElement.Key;
                var expected = _singleElement;
                var actual = target.Get<Element>(id);
                Assert.AreNotSame(expected, actual);
                Assert.AreEqual(expected, actual);

                id = _dependentElement.Key;
                expected = _dependentElement;
                actual = target.Get<Element>(id);
                Assert.AreNotSame(expected, actual);
                Assert.AreEqual(expected, actual);

                id = Guid.NewGuid();
                expected = null;
                actual = target.Get<Element>(id);                
                Assert.AreEqual(expected, actual);
            }
        }       

        [TestMethod]
        [TestCategory("BasePoolTest")]
        public void GetResourceTest()
        {
            GetResourceTestHelper<Model>();
            GetResourceTestHelper<Element>();
        }

        /// <summary>
        ///A test for Get
        ///</summary>
        public void GetResourceTestByQueryHelper<T>()
            where T : class
        {

            ITypedPool target = CreateITransientCachier();
            if (typeof(T) == typeof(Model))
            {
             
                var expectedModel = _dependentModel.Key;
                var actualModel = target.Query<Model>(new UriQuery{{"Element",_dependentElement.Name}}).First();
                Assert.AreNotSame(expectedModel, actualModel);
                Assert.AreEqual(expectedModel, actualModel);
              

                var nolModels = target.Query<Model>(new UriQuery { { "Element", _singleElement.Name } });
                Assert.IsTrue(!nolModels.Any());

                try
                {
                    var res = target.Query<Model>(new UriQuery("default=true"));
                    Assert.IsTrue(res.Count() ==1);
                }
                catch (InvalidOperationException)
                {
                   
                }
               
            }

            if (typeof(T) == typeof(Element))
            {
                var expectedElement = _dependentElement.Key;
                var actualElement = target.Query<Element>(new UriQuery { { "modelName", _dependentModel.Name } }).First();
                Assert.AreNotSame(expectedElement, actualElement);
                Assert.AreEqual(expectedElement, actualElement);

                var actualElements = target.Query<Element>(
                    new UriQuery { { "ModelName", _dependentModel.Name } , {"ModelId",_singleModel.Key.ToString()} }).ToArray();
                Assert.AreEqual(1,actualElements.Count());
                Assert.AreEqual(expectedElement, actualElements[0]);

                var nolElements = target.Query<Element>(new UriQuery { { "modelName", _singleModel.Name } });
                Assert.IsTrue(!nolElements.Any());

                var emptyKey = target.Query<Element>(new UriQuery("badquery=true"));
                Assert.AreEqual(Guid.Empty,emptyKey.First());
            }
        }

        public void GetResourceTestByArgsHelper<T>() where T : class
        {

            ITypedPool target = CreateITransientCachier();
            if (typeof(T) == typeof(Model))
            {

                var expectedModel = _dependentModel.Key;
                object q = target.Spec(Element: _dependentElement.Name);
                var t = target.Find<Model>(q);
                var actualModel = t.First();
                Assert.AreNotSame(expectedModel, actualModel);
                Assert.AreEqual(expectedModel, actualModel);
                q = target.Spec(Element: _singleElement.Name);
                var nolModels = target.Find<Model>(q);
                Assert.IsTrue(!nolModels.Any());

                try
                {
                    q = target.Spec(Default: true);
                    var res = target.Find<Model>(q);
                    Assert.IsTrue(res.Count() == 1);
                }
                catch (InvalidOperationException)
                {

                }
            }

            if (typeof(T) == typeof(Element))
            {
                var expectedElement = _dependentElement.Key;
                object query = target.Spec(modelName: _dependentModel.Name);                
                var actualElement = target.Find<Element>(query).First();
                Assert.AreNotSame(expectedElement, actualElement);
                Assert.AreEqual(expectedElement, actualElement);
                dynamic dquery = target.Spec(ModelName: _dependentModel.Name);
                dquery.ModelId = _singleModel.Key;
                var actualElements = target.Find<Element>(query).ToArray();
                Assert.AreEqual(1, actualElements.Count());
                Assert.AreEqual(expectedElement, actualElements[0]);
                query = target.Spec(modelName: _singleModel.Name);
                var nolElements = target.Find<Element>(query);
                Assert.IsTrue(!nolElements.Any());
                query = target.Spec(badquery: true);
                var emptyKey = target.Find<Element>(query);
                Assert.AreEqual(Guid.Empty, emptyKey.First());
            }
        }

        [TestMethod]
        [TestCategory("BasePoolTest")]
        public void GetResourceByQueryTest()
        {
            GetResourceTestByQueryHelper<Model>();
            GetResourceTestByQueryHelper<Element>();
        }

        [TestMethod]
        [TestCategory("BasePoolTest")]
        public void GetResourceByMethodQueryTest()
        {
            GetResourceTestByArgsHelper<Model>();
            GetResourceTestByArgsHelper<Element>();
        }
      
        /// <summary>
        ///A test for Get
        ///</summary>
        [TestMethod]
        [TestCategory("BasePoolTest")]
        public void GetResourceByNameTest()
        {
            var target = CreateNamedTransientCachier(); 
            string name = "Model"; 
            Guid id = _singleModel.Key;
            object expected = _singleModel;
            object actual = target.Get(name, id);
            Assert.AreNotSame(expected, actual);
            Assert.AreEqual(expected, actual);
            
            name = "Element";
            id = _singleElement.Key;
            expected = _singleElement;
            actual = target.Get(name, id);
            Assert.AreNotSame(expected, actual);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for GetLater
        ///</summary>
        public void GetResourceDeferredTestHelper<T>()
            where T : class
        {
            var target = CreateITransientCachier();          
            var ids = new[] { _singleModel.Key, _dependentModel.Key };
            var expected1 = _singleModel;
            var expected2 = _dependentModel;

            var actual1 = target.GetLater<Model>(ids[0]);
            var actual2 = target.GetLater<Model>(ids[1]);
            Assert.AreEqual(expected1, actual1.Value);
            Assert.AreEqual(expected2, actual2.Value);
            Assert.AreNotSame(expected1, actual1.Value);
            Assert.AreNotSame(expected2, actual2.Value);
        
        }

        [TestMethod()]
        [TestCategory("BasePoolTest")]
        public void GetResourceDeferredTest()
        {
            GetResourceDeferredTestHelper<GenericParameterHelper>();
        }

        /// <summary>
        ///A test for GetLater
        ///</summary>
        [TestMethod]
        [TestCategory("BasePoolTest")]
        public void GetResourceDeferredByNameTest()
        {
            var target = CreateNamedTransientCachier(); 
            string name = "Model";
            var ids = new[] {_singleModel.Key, _dependentModel.Key};
            var expected1 = _singleModel;
            var expected2 = _dependentModel;
            
            var actual1 = target.GetLater(name, ids[0]);
            var actual2 = target.GetLater(name, ids[1]);
            Assert.AreNotSame(expected1, actual1.Value);
            Assert.AreNotSame(expected2, actual2.Value);
            Assert.AreEqual(expected1, actual1.Value);
            Assert.AreEqual(expected2, actual2.Value);        
        }

        /// <summary>
        ///A test for GetNameByType
        ///</summary>
        [TestMethod]
        [TestCategory("BasePoolTest")]
        public void GetResourceNameByTypeTest()
        {
            var target = CreateNamedTransientCachier(); 
            Type type = typeof(Model); 
            string expected = "Model";
            string actual = target.GetNameByType(type);
            Assert.AreNotSame(expected, actual);  
            Assert.AreEqual(expected, actual);           

            try
            {
                type = typeof (object);
                target.GetNameByType(type);
                Assert.Fail();
            }
            catch (KeyNotFoundException)
            {
                                
            }                       
        }

        /// <summary>
        ///A test for GetNameByType
        ///</summary>
        [TestMethod()]
        [TestCategory("BasePoolTest")]
        public void PostResourceTest()
        {
            GetResourceTest();
            ITypedPool target = CreateITransientCachier();

            target.Post(_dependentModel.Key,_singleModel);
            var actModel = target.Get<Model>(_dependentModel.Key);
            Assert.AreEqual(_singleModel.Key,actModel.Key);
            Assert.AreEqual(_singleModel.Name, actModel.Name);
            target.Post<Model>(_dependentModel.Key, null);
            actModel = target.Get<Model>(_dependentModel.Key);
            Assert.AreEqual(null, actModel);

            target.Post(_dependentElement.Key, _singleElement);
            var actElement = target.Get<Element>(_dependentElement.Key);
            Assert.AreEqual(_singleElement.Key, actElement.Key);
            Assert.AreEqual(_singleElement.Name, actElement.Name);
            target.Post<Element>(_dependentElement.Key, null);
            actElement = target.Get<Element>(_dependentElement.Key);
            Assert.AreEqual(null, actElement);
            target.Post<Element>(_dependentElement.Key, null);//удаление отсуствующего ключа должно работать без ошибок

            target.Post(_dependentElement.Key,_dependentElement);
            target.Post(_dependentModel.Key,_dependentModel);

            GetResourceTest();
        }

        /// <summary>
        ///A test for GetNameByType
        ///</summary>
        [TestMethod()]
        [TestCategory("BasePoolTest")]
        public void PostResourceByNameTest()
        {
            GetResourceTest();
            var target = CreateNamedTransientCachier();

            target.Post("Model",_dependentModel.Key, _singleModel);
            var actModel = target.Get("Model",_dependentModel.Key) as Model;
            Assert.AreEqual(_singleModel.Key, actModel.Key);
            Assert.AreEqual(_singleModel.Name, actModel.Name);
            target.Post("Model", _dependentModel.Key, null);
            actModel = target.Get("Model", _dependentModel.Key) as Model;
            Assert.AreEqual(null, actModel);

            target.Post("Element",_dependentElement.Key, _singleElement);
            var actElement = target.Get("Element", _dependentElement.Key) as Element;
            Assert.AreEqual(_singleElement.Key, actElement.Key);
            Assert.AreEqual(_singleElement.Name, actElement.Name);
            target.Post("Element", _dependentElement.Key, null);
            actElement = target.Get("Element", _dependentElement.Key) as Element;
            Assert.AreEqual(null, actElement);

            target.Post("Element", _dependentElement.Key, _dependentElement);
            target.Post("Model", _dependentModel.Key, _dependentModel);

            GetResourceTest();
        }

        [TestMethod]
        [TestCategory("BasePoolTest")]
        public void SimpleDependencyTest()
        {
              var target = CreateITransientCachier();
              var elemDependent = target.Get<ElementDeriver>(_singleElement.Key);
              Assert.AreEqual(_singleElement.Name,elemDependent.Name);
              _singleElement.Name = "DependTest";
              target.Post(_singleElement.Key, _singleElement);
              elemDependent = target.Get<ElementDeriver>(_singleElement.Key);
              Assert.AreEqual(_singleElement.Name, elemDependent.Name);

             
              var modelDependent = target.Get<ModelDeriver>(_singleModel.Key);
              Assert.AreEqual(_singleModel.Name, modelDependent.Name);
              var oldName = _singleModel.Name;
              _singleModel.Name = "DependTest";
              target.Post(_singleModel.Key, _singleModel);
              modelDependent = target.Get<ModelDeriver>(_singleModel.Key);
              Assert.AreEqual(oldName, modelDependent.Name);
        }


        [TestMethod]
        [TestCategory("BasePoolTest")]
        public void QueriedDependencyTest()
        {
            var target = CreateITransientCachier();
            var elemBlock1 = target.Get<ElementsInModel>(_dependentModel.Key);
            var elemBlock2 = target.Get<ElementsInModel>(_singleModel.Key);
            Assert.AreEqual(0, elemBlock2.Elements.Count());
            Assert.AreEqual(1,elemBlock1.Elements.Count());
            Assert.AreEqual(_dependentElement.Key, elemBlock1.Elements.Single());

            _dependentElement.Model = _singleModel.Key;
            target.Post(_dependentElement.Key, _dependentElement);
            elemBlock1 = target.Get<ElementsInModel>(_dependentModel.Key);
            elemBlock2 = target.Get<ElementsInModel>(_singleModel.Key);
         
            Assert.AreEqual(0, elemBlock1.Elements.Count());
            Assert.AreEqual(1, elemBlock2.Elements.Count());
            Assert.AreEqual(_dependentElement.Key, elemBlock2.Elements.Single());        
        }

        [TestMethod]
        [TestCategory("BasePoolTest")]
        public void SeriesTest()
        {
            var target = CreateITransientCachier();
            var elements = MockHelper.GetRandomSequence<Guid>(300).ToDictionary(k => k, k => new Element { Key = k, Name = k.ToString() });

            foreach(var element in elements)
               target.Post(element.Key,element.Value);

            var gotElements = target.Get<Element>(elements.Select(k => k.Key));
            MockHelper.CheckSequences(elements.OrderBy(k => k.Key).Select(k => k.Value).ToArray(), gotElements.OrderBy(k => k.Key).ToArray());
            Assert.IsTrue(ElementGetter.WasChanged);

            ElementGetter.WasChanged = false;
            gotElements = target.Get<Element>(elements.Select(k => k.Key));
            MockHelper.CheckSequences(elements.OrderBy(k => k.Key).Select(k => k.Value).ToArray(), gotElements.OrderBy(k => k.Key).ToArray());
            Assert.IsTrue(!ElementGetter.WasChanged);
        }


        protected Assembly[] AssembliesWhichShouldProvideExport
        {
            get { return new[] { typeof(Model).Assembly,typeof(ITypedPool).Assembly }; } 
        }

        [TestInitialize]
        public void Preparation()
        {
            _container = MockHelper.InitializeContainer(AssembliesWhichShouldProvideExport,new Type[0]);

            _singleModel = new Model {Element = Guid.Empty, Key = Guid.NewGuid(),Name = "SingleModel"};
            _singleElement = new Element() { Model = Guid.Empty, Key = Guid.NewGuid(), Name = "SingleElement" };
            _dependentModel = new Model { Element = Guid.NewGuid(), Key = Guid.NewGuid(), Name = "DepModel" };
            _dependentElement = new Element() { Model = _dependentModel.Key, Key = _dependentModel.Element, Name = "DepElement" };
            var pool = new ResourcePool();
            pool.Models.Add(_singleModel.Key, _singleModel);
            pool.Models.Add(_dependentModel.Key, _dependentModel);
            pool.Elements.Add(_singleElement.Key, _singleElement);
            pool.Elements.Add(_dependentElement.Key, _dependentElement);
            _container.ComposeExportedValue(pool);
            _container.ComposeExportedValue(_container);
            Settings.NoCacheGarbageChecking = true;
        }

        [TestCleanup]
        public void Shutdown()
        {
            _container.Dispose();
        }

    }
}
