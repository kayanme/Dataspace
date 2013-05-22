using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Services
{
    [Export]
    internal class CompositionService:IPartImportsSatisfiedNotification 
    {
        [Import(AllowDefault = true)]
        private CompositionContainer _container;
        public CompositionContainer Container{get { return _container; }}

        internal void SetContainer(CompositionContainer container)
        {
            Contract.Requires(container != null);
            Debug.Assert(container!=null);
            if (container == null)
                throw new ArgumentNullException("container");
            _container = container;
        }

        public void OnImportsSatisfied()
        {
            if (_container == null)
                _container = new CompositionContainer(new AssemblyCatalog(typeof(CompositionService).Assembly));
        }
    }
}
