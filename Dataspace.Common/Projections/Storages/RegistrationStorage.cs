using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Projections.Storages.SchemaBuilders;
using Dataspace.Common.Data;

namespace Dataspace.Common.Projections.Storages
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class RegistrationStorage
    {

        #region Imports

#pragma warning disable 0649
        [Import(AllowRecomposition = false)] private IAnnouncerSubscriptorInt _subscriptor;
#pragma warning restore 0649

        #endregion

        public const string Dataspace = "http://metaspace.org/DataSchema";

        public const string DefinitlyNoChildren = "DefinitlyNoChildren";

        //регистрации для связей "тип ресурса - имя ресурса"
        private readonly Dictionary<Type, string> _registrations = new Dictionary<Type, string>();
        private readonly Dictionary<string, Type> _backRegistrations = new Dictionary<string, Type>();

        /// <summary>
        /// Добавляет записи о регистрации типа-ресурса.
        /// </summary>
        /// <param name="type">Тип.</param>
        /// <param name="name">Имя.</param>
        public void AddRegistrations(Type type, string name)
        {
            try
            {
                _registrations.Add(type, name);
                _backRegistrations.Add(name, type);
                _subscriptor.AddResourceName(name);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException("Тип регистрируется два раза:" + type);
            }
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
            return _registrations.ContainsKey(type);
        }

        public bool IsResourceRegistered(string name)
        {
            return _backRegistrations.ContainsKey(name);
        }

        public Type this[string name]
        {
            get { return _backRegistrations[name]; }
        }

        public string this[Type type]
        {
            get { return _registrations[type]; }
        }

        public void FlushRegistraions()
        {
            _registrations.Clear();
            _backRegistrations.Clear();
            _subscriptor.Clear();
        }

        public IEnumerable<Type> GetRegisteredTypes()
        {
            return _registrations.Keys;
        }

        public XmlSchema GetDataSchemas(XmlSchema querySchema)
        {
            var builder = new DataSchemaBuilder();
            return builder.GetDataSchemas(querySchema, _registrations);
        }


    }
}
