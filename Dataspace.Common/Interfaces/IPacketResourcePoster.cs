using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dataspace.Common.Announcements;
using Dataspace.Common.Data;

namespace Dataspace.Common.Interfaces
{
    public interface IPacketResourcePoster
    {
        /// <summary>
        /// Записывает блок ресурсов.
        /// </summary>
        /// <returns>Незаписанные по каким-то причинам ресурсы - будут дозаписаны поодиночке</returns>
        IEnumerable<DataRecord> PostResourceBlock(IEnumerable<DataRecord> resourceBlock);
    }
}
