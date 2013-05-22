using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Dataspace.Common.Data;

namespace Dataspace.Common.Interfaces
{

    [ContractClass(typeof(ICacheServicingContract))]
    public interface ICacheServicing
    {
        void CachePanic();
        void Initialize();
        void Initialize(Settings settings,CompositionContainer container = null);
        bool IsInitialized { get; }
    }

    [ContractClassFor(typeof(ICacheServicing))]
    public class ICacheServicingContract:ICacheServicing
    {
        public void CachePanic()
        {
           
        }

        public void Initialize()
        {
          Contract.Ensures(IsInitialized);
        }

        public void Initialize(Settings settings, CompositionContainer container = null)
        {
            Contract.Requires(settings != null);
            Contract.Ensures(IsInitialized);
        }

        public bool IsInitialized { get; set; }
    }
}
