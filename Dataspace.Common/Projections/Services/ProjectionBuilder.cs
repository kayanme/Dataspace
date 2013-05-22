using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using Dataspace.Common;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Projections;
using Dataspace.Common.Projections.Classes;
using Dataspace.Common.Projections.Classes.Plan;
using Dataspace.Common.Projections.Services;
using Dataspace.Common.Projections.Storages;
using Dataspace.Common.Services;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;
using Server.Modules.HierarchiesModule;
using Server.Modules.HierarchiesModule.Queries;


namespace Dataspace.Common.Hierarchies
{
    [Export(typeof(IProjectionsCollector))]
    [Export]
    [Export(typeof(IInitialize))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ProjectionBuilder:IProjectionsCollector,IInitialize
    {
#pragma warning disable 0649
        [Import]
        private SchemaStorage _storage;               

        [Import]
        private ProjectionStorage _projStorage;

        [Import]
        private FramingPlanBuilder _planBuilder;

        [Import]
        private PlanFiller _planFiller;

        [Import]
        private Streamer<Stream> _streamer;

        [Import]
        private ICacheServicing _service;

        [Import]
        private QueryStorage _queryStorage;

        [Import]
        private RegistrationStorage _registrationStorage;
#pragma warning restore 0649
                       
        //внутренний для тестирования
        public int Order { get { return 14; } }
        void IInitialize.Initialize()
        {
            _storage.Initialize();          
        }

        private void CheckInitialization()
        {
            if (!_service.IsInitialized)
                _service.Initialize();
        }
       

        //внутренний для тестирования
        internal FramingPlan ChoosePlan(ProjectionElement root, int depth,ParameterNames pars)
        {
           return _planBuilder.MakePlan(root, depth,pars);
        }

        //внутренний для тестирования
        internal TOutStream RealisePlan<TOutStream>(FramingPlan plan, Guid rootKey, string nmspc, int maxDepth, Streamer<TOutStream> streamer, Dictionary<string, object> parameters)
        {
            var frame = plan.MakeFrame(rootKey, maxDepth, parameters);
            var filledFrame = _planFiller.FillFrame(frame);
            var stream = streamer.StreamFilledFrames(filledFrame, nmspc);
            return stream;
        }

        public Stream GetProjection(Guid id, string name, string nmspace, HierarchyQuery query = null)
        {
            CheckInitialization();
            try
            {
                _storage.StartReading();
                var root = _projStorage.FindElement(name, nmspace);
                if (root == null)
                    throw new ArgumentException("Невозможно получить проекцию - не найден переданный элемент");
                var procQuery = new ProcessorQuery(query);
                var parametersQuery = procQuery.Query
                    .ByDefault(
                        k2 => k2.ToDictionary(k => k.Key, k => k.Value, StringComparer.InvariantCultureIgnoreCase),
                        new Dictionary<string, string>());
                Debug.Assert(root != null);
                var parameterNames = new ParameterNames(parametersQuery.Select(k => k.Key));
                var plan = ChoosePlan(root, procQuery.LeftDepth, parameterNames);
                var conversedParameters = plan.ConverseArguments(parametersQuery);
                var stream = RealisePlan(plan, id, nmspace, procQuery.LeftDepth, _streamer, conversedParameters);
                stream.Position = 0;
                return stream;
            }
            finally
            {
                _storage.StopReading();
            }
        }

        public XmlSchemaSet GetSchemas()
        {
            var querySchema = _queryStorage.GetQuerySchema();
            var dataSchema = _registrationStorage.GetDataSchemas(querySchema);
            var set = new XmlSchemaSet();         
            set.Add(querySchema);
            set.Add(dataSchema);                  
            set.Compile();                   
            return set;

        }

     
    }
}
