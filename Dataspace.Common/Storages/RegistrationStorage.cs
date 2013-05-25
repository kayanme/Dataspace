using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Projections.Storages.SchemaBuilders;
using Dataspace.Common.Data;
using Dataspace.Common.ServiceResources;
using Dataspace.Common.Utility;

namespace Dataspace.Common.Projections.Storages
{
    [Export]
    [Export(typeof(IInitialize))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class RegistrationStorage:IInitialize 
    {

        #region Imports

#pragma warning disable 0649
        [Import(AllowRecomposition = false)] private IAnnouncerSubscriptorInt _subscriptor;
#pragma warning restore 0649

        //это все регистраторы ресурсов. Каждый ресурс, с которым требуется работать, должен быть зарегистрирован.
        //где располагать регистраторы - дело хозяйское
        [ImportMany(typeof(ResourceRegistrator))]
        private IEnumerable<ResourceRegistrator> _registrators;
     
        #endregion

        public const string Dataspace = "http://metaspace.org/DataSchema";

        public const string DefinitlyNoChildren = "DefinitlyNoChildren";

        //регистрации для связей "тип ресурса - имя ресурса"
        private readonly IndexedCollection<Registration>  _registrations = new IndexedCollection<Registration>(k=>k.ResourceKey,k=>k.ResourceName,k=>k.ResourceType);

        private static void TestTypeSerialization(Type type)
        {

            var isSerializable = Attribute.IsDefined(type, typeof(SerializableAttribute));

            Debug.Assert(isSerializable, "Ресурс (" + type.Name + ") должен быть сериализуемым и помеченным атрибутом [Serializable]");
            if (!isSerializable)
                throw new InvalidOperationException("Ресурс (" + type.Name + ") должен быть сериализуемым и помеченным атрибутом [Serializable]");

            using (var s = new MemoryStream())
            {
                try
                {
                    new BinaryFormatter().Serialize(s, Activator.CreateInstance(type));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Тестовая сериализация ресурса " + type.FullName + " неудачна.", ex);
                }

            }
        }
   
        /// <summary>
        /// Добавляет записи о регистрации типа-ресурса.
        /// </summary>
        /// <param name="type">Тип.</param>
        /// <param name="name">Имя.</param>
        private void AddRegistrations(Type type, string name,bool isSecuritized,bool isCacheData)
        {
            try
            {
                _registrations.Add(new Registration(name, type, isSecuritized, isCacheData));                
                _subscriptor.AddResourceName(name);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException("Тип регистрируется два раза:" + type);
            }
        }

        public IEnumerable<Registration>  AllRegistrations
        {
            get { return _registrations; }
        }

        /// <summary>
        /// Возвращает, зарегисрирован ли переданный тип как ресурс.
        /// </summary>
        /// <param name="type">Тип.</param>
        /// <returns>
        ///   <c>true</c> if [is resource registered] [the specified type]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsResourceRegistered(Type type)
        {
            return _registrations.Any(k=>k.ResourceType == type);
        }

        public bool IsResourceRegistered(Guid id)
        {
            return _registrations.Any(k => k.ResourceKey == id);
        }

        public bool IsResourceRegistered(string name)
        {
            return _registrations.Any(k=>k.ResourceName == name);
        }

        public Registration this[string name]
        {
            get { return _registrations.First(k=>k.ResourceName == name); }
        }

        public Registration this[Type type]
        {
            get { return _registrations.First(k=>k.ResourceType == type); }
        }

        public Registration this[Guid id]
        {
            get { return _registrations.FirstOrDefault(k => k.ResourceKey == id); }
        }

        public void FlushRegistraions()
        {
            _registrations.Clear();           
            _subscriptor.Clear();
        }

        public IEnumerable<Type> GetRegisteredTypes()
        {
            return _registrations.Select(k=>k.ResourceType);
        }

        public XmlSchema GetDataSchemas(XmlSchema querySchema)
        {
            var builder = new DataSchemaBuilder();
            return builder.GetDataSchemas(querySchema, _registrations);
        }


        public int Order { get { return 1; } }
        public void Initialize()
        {
            foreach (var type in _registrators.SelectMany(k => k.ResourceTypesInt))
            {
                try
                {
                    string name;
                    bool isCacheData;
                    //определение ресурса
                    var definition = Attribute.GetCustomAttribute(type, typeof(ResourceAttribute)) as ResourceAttribute;
                    //или кэш-данных
                    var cacheData = Attribute.GetCustomAttribute(type, typeof(CachingDataAttribute)) as CachingDataAttribute;
                    //защищен ли ресурс настройками безопасности
                    var securitized = Attribute.GetCustomAttribute(type, typeof(SecuritizedAttribute)) as SecuritizedAttribute;
                    bool isSecuritized = securitized != null;
                    if (definition != null)//если это ресурс
                    {
                        Debug.Assert(cacheData == null, "Либо ресурс, либо данные для кэширования!");
                        if (cacheData != null)
                            throw new InvalidOperationException("Либо ресурс, либо данные для кэширования!");

                        TestTypeSerialization(type);
                        name = definition.Name;
                        isCacheData = false;

                    }
                    else if (cacheData != null)//если это кэш-данные
                    {
                        name = cacheData.Name;
                        isCacheData = true;
                    }
                    else
                    {
                        Debug.Fail("Регистрируемый объект должен быть помечен либо атрибутом [Resource], либо атрибутом [CacheData]. ");
                        throw new InvalidOperationException("Регистрируемый объект должен быть помечен либо атрибутом [Resource], либо атрибутом [CacheData]. ");
                    }
                    AddRegistrations(type,name,isSecuritized,isCacheData);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Ошибка подготовки типа:" + type.Name, ex);
                }
            }       
        }
    }
}
