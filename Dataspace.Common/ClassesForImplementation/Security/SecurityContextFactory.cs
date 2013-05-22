using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Dataspace.Common.Interfaces;

namespace Dataspace.Common.ClassesForImplementation.Security
{
    /// <summary>
    /// Фабрика, получающая контекст безопасности в контексте вызова методов кэшера.
    /// </summary>
    [ContractClass(typeof(SecurityContextFactoryContract))]
    public abstract class SecurityContextFactory
    {

        [Import]
        protected ITypedPool Cachier { get; private set; }

        internal void SetCache(ITypedPool cache)
        {
            Cachier = cache;
        }

        /// <summary>
        /// Получение контекста.
        /// </summary>
        /// <returns></returns>
        public abstract ISecurityContext GetContext();
    }

    [ContractClassFor(typeof(SecurityContextFactory))]
    public abstract class SecurityContextFactoryContract : SecurityContextFactory
    {
        public override ISecurityContext GetContext()
        {
            Contract.Ensures(Contract.Result<ISecurityContext>() != null);
            return null;
        }
    }
}
