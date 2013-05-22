using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Exceptions
{
    [Serializable]
    public sealed class PostException:ApplicationException
    {
        private const string Error = "Error posting resource {0}";

        
        public PostException()
        {

        }

        public PostException(string resourceName,Exception inner):base(string.Format(Error,resourceName),inner)
        {
            
        }
        
       
    }
}
