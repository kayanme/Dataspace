using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Dataspace.Common.Data;

namespace Dataspace.Common.ClassesForImplementation
{
    /// <summary>
    /// Класс для реализации очереди сообщение об обновлении нижележащего слоя.
    /// </summary>
    [ContractClass(typeof(AnnouncerDownlinkContract))]
    public abstract class AnnouncerDownlink : IObservable<ResourceDescription>,IDisposable
    {
        public abstract IDisposable Subscribe(IObserver<ResourceDescription> observer);

        public abstract void Dispose();
    }

    [ContractClassFor(typeof(AnnouncerDownlink))]
    public class AnnouncerDownlinkContract : AnnouncerDownlink
    {
        public override IDisposable Subscribe(IObserver<ResourceDescription> observer)
        {
            Contract.Requires(observer!=null);
            Contract.Ensures(Contract.Result<IDisposable>() !=null);
            return null;
        }

        public override void Dispose()
        {            
        }
    }
}
