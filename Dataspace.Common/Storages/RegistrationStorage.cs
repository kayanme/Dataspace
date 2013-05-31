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
        [Import(AllowRecomposition = false)]
        private IAnnouncerSubscriptorInt _subscriptor;

        //это все регистраторы ресурсов. Каждый ресурс, с которым требуется работать, должен быть зарегистрирован.
        //где располагать регистраторы - дело хозяйское
        [ImportMany(typeof(ResourceRegistrator))]
        private IEnumerable<ResourceRegistrator> _registrators;
#pragma warning restore 0649
            
        #endregion

        public const string Dataspace = "http://metaspace.org/DataSchema";

        public const string DefinitlyNoChildren = "DefinitlyNoChildren";

        //регистрации для связей "тип ресурса - имя ресурса"
        private readonly IndexedCollection<Registration>  _registrations = new IndexedCollection<Registration>(k=>k.ResourceKey,k=>k.ResourceName,k=>k.ResourceType);

        private static void TestTypeSerialization(Type type)
        {

            var isSerializable = type.IsSerializable;

            Debug.Assert(isSerializable,string.Format("Ресурс ({0}) должен быть сериализуемым",type.Name));
            if (!isSerializable)
                throw new InvalidOperationException(string.Format("Ресурс ({0}) должен быть сериализуемым", type.Name));

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
        /// <param name="registration">Регистрация</param>
        /// <exception cref="System.InvalidOperationException">Тип регистрируется два раза: + registration.ResourceType</exception>
        private void AddRegistrations(Registration registration)
        {
            try
            {
                _registrations.Add(registration);
                _subscriptor.AddResourceName(registration.ResourceName);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException("Тип регистрируется два раза:" + registration.ResourceType);
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
            get { return _registrations.FirstOrDefault(k=>k.ResourceName == name); }
        }

        public Registration this[Type type]
        {
            get { return _registrations.FirstOrDefault(k => k.ResourceType == type); }
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
            foreach (var registration in _registrators.SelectMany(k => k.GetRegistrations()))
            {
                if (!registration.IsCacheData)
                  TestTypeSerialization(registration.ResourceType);
                AddRegistrations(registration);
            }       
        }
    }
}
