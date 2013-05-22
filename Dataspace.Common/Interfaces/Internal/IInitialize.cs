namespace Dataspace.Common.Interfaces.Internal
{
    internal interface IInitialize
    {
        int Order { get; }
        void Initialize();       
    }
}
