using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.Data;

namespace Dataspace.Common.ClassesForImplementation
{
    /// <summary>
    /// Класс для формирования очереди сообщений об обновлениях для вышележащих слоев.
    /// </summary>
    public abstract class AnnouncerUplink : IObserver<ResourceDescription>
    {
        public abstract void OnNext(ResourceDescription value);
        public abstract void OnError(Exception error);
        public abstract void OnCompleted();
    }
}
