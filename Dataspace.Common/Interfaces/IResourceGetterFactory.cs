using Dataspace.Common.ClassesForImplementation;

namespace Dataspace.Common.Interfaces
{
    public interface IResourceGetterFactory
    {
        ResourceGetter<T> CreateGetter<T>() where T : class;
    }
}
