using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
[assembly: InternalsVisibleTo("ResourceProxy")]
namespace Indusoft.Common.ResourceLayer.Interfaces.Internal
{

   
    internal interface IProxyWithLazyItem
    {
        object GetItem();
    }
}
