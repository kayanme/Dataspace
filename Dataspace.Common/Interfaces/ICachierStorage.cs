using System;

namespace Dataspace.Common.Interfaces
{
    public interface ICachierStorage<T>
    {
        object RetrieveByFunc(T id, Func<T, object> value);
        void Push(T id, object value);
        void SetUpdateNecessity(T id);
        bool HasActualValue(T id);
        void StartUpdates();
        void StopUpdates();
        void Clear();
    }
}
