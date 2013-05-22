using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Projections.Services
{
    internal abstract class ProjectionWriter<TStream>
    {
       
        public abstract void StartWritingNode(string name, Guid id);

        public abstract void WriteAttribute(string name, object value);

        public abstract void EndWritingNode();

        public abstract TStream Open(string nmspc);

        public abstract void Close();
    }
}
