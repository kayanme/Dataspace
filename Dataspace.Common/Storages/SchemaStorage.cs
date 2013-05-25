using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Schema;
using Dataspace.Common.Hierarchies;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Projections.Storages;
using Dataspace.Common.Services;
using Dataspace.Common.Hierarchies;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Projections.Storages;
using Dataspace.Common.Services;

namespace Dataspace.Common.Projections
{
    [Export]
    [Export(typeof(ISchemeControlling))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class SchemaStorage : ISchemeControlling
    {       
     
#pragma warning disable 0649
        [ImportMany] 
        private IEnumerable<ISchemeProvider> _schemas;

    
        [Import]
        private ProjectionBuilder _builder;

        [Import]
        private ProjectionStorage _projStorage;
#pragma warning restore 0649

        private readonly ReaderWriterLockSlim _schemeChanginLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private XmlSchemaSet _allSchemas;

        public XmlSchemaSet AllSchemas
        {
            get
            {
                SpinWait.SpinUntil(() => _allSchemas != null);
                try
                {
                    _schemeChanginLock.EnterReadLock();
                    {
                        Debug.Assert(_allSchemas != null);
                        return _allSchemas;
                    }
                }
                finally
                {

                    _schemeChanginLock.ExitReadLock();
                }
            }
        }

        public void StartReading()
        {
            _schemeChanginLock.EnterReadLock();
        }

        public void StopReading()
        {
            _schemeChanginLock.ExitReadLock();
        }

        public void Initialize()
        {
            try
            {
                _schemeChanginLock.EnterWriteLock();
                Debug.Assert(_schemas != null);
                var set = _builder.GetSchemas();
                _allSchemas =
                    _schemas.Aggregate(set,
                                       (a, s) =>
                                           {
                                               var readScheme = s.GetReadScheme();
                                               Debug.Assert(readScheme != null);
                                               a.Add(readScheme);
                                               return a;
                                           }
                        );
                _allSchemas.ValidationEventHandler += _allSchemas_ValidationEventHandler;
                _allSchemas.Compile();
                PrepareElements();
            }
            finally
            {
                _schemeChanginLock.ExitWriteLock();
            }

        }

        void _allSchemas_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            throw new InvalidOperationException("Некорректная схема",e.Exception);
        }

        private void PrepareElements()
        {
            _projStorage.Clean();
            var elements = AllSchemas.GlobalElements.Values.OfType<XmlSchemaElement>();
            _projStorage.ProcessElements(elements);   
        }

        public void IntegrateNewSchemas(IEnumerable<XmlSchema> schemas)
        {
            try
            {
                _schemeChanginLock.EnterWriteLock(); //эти замки рекурсивны, так что можно использовать
                foreach (var xmlSchema in schemas)
                {
                    IntegrateNewSchema(xmlSchema);
                }
                PrepareElements();
            }
            finally
            {
                _schemeChanginLock.ExitWriteLock();
            }
        }

        private void IntegrateNewSchema(XmlSchema schema)
        {
            Contract.Requires(schema != null);

            if (schema.TargetNamespace == QueryStorage.QuerySpace
                || schema.TargetNamespace == RegistrationStorage.Dataspace)
                throw new InvalidOperationException("Нельзя добавить схему с данным пространством имен");
            if (_allSchemas == null)
                Initialize();

            Debug.Assert(_allSchemas != null);
            var presentSchema = _allSchemas.Schemas(schema.TargetNamespace).OfType<XmlSchema>().FirstOrDefault();
            if (presentSchema != null)
            {
                _allSchemas.Remove(presentSchema);

            }
            _allSchemas.Add(schema);
            _allSchemas.Compile();


        }

        public void RemoveSchema(string space)
        {
            Contract.Requires(!string.IsNullOrEmpty(space));
            if (space == QueryStorage.QuerySpace
               || space == RegistrationStorage.Dataspace)
                throw new InvalidOperationException("Нельзя удалить схему с данным пространством имен");
            try
            {
                _schemeChanginLock.EnterWriteLock();
                if (_allSchemas == null)
                    Initialize();
                Debug.Assert(_allSchemas != null);
                var deletingSchema = _allSchemas.Schemas(space).OfType<XmlSchema>().FirstOrDefault();
                if (deletingSchema == null)
                    throw new InvalidOperationException("Нет схемы " + space + " для удаления");
                _allSchemas.Remove(deletingSchema);
                _allSchemas.Compile();
                PrepareElements();
            }
            finally
            {
                _schemeChanginLock.ExitWriteLock();
            }
        }

        public XmlSchemaSet GetSchemas()
        {
            var copiedSet =
                AllSchemas.Schemas()
                    .OfType<XmlSchema>()
                    .Select(k =>
                                {
                                    using (var stream = new MemoryStream())
                                    {
                                        k.Write(stream);
                                        stream.Position = 0;
                                        return XmlSchema.Read(stream,
                                                              _allSchemas_ValidationEventHandler);
                                    }
                                })
                    .Aggregate(new XmlSchemaSet(),
                                             (a, s) =>
                                                 {

                                                     a.Add(s);
                                                     return a;
                                                 });
            return copiedSet;

        }

        public void Deinitialize()
        {
            try
            {
                _schemeChanginLock.EnterWriteLock();
                _allSchemas = null;
                _projStorage.Clean();
            }
            finally
            {
                _schemeChanginLock.ExitWriteLock();
            }
        }

       
    }
}
