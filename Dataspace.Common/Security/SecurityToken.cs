using System;

namespace Dataspace.Common.Security
{
    [Serializable]
    public class SecurityToken
    {
        public bool CanRead{ get; private set;}

        public bool CanWrite { get; private set; }

        internal SecurityToken(bool canRead,bool canWrite)
        {
            CanRead = canRead;
            CanWrite = canWrite;
        }
    }
}
