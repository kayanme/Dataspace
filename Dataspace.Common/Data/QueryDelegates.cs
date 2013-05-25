using System;
using System.Collections.Generic;

namespace Dataspace.Common.Data
{
    public delegate IEnumerable<Guid> FormedQuery(params object[] args);
 
    public delegate IEnumerable<Guid> QueryForSingleParentResource(Guid resource, params object[] args);

    public delegate IEnumerable<KeyValuePair<Guid,IEnumerable<Guid>>> QueryForMultipleParentResource(IEnumerable<Guid> resources, params object[] args);


}