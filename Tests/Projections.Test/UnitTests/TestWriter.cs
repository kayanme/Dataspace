using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.Projections.Services;

namespace Projections.Test
{

    [Export(typeof(ProjectionWriter<StringBuilder>))]
    internal class TestWriter:ProjectionWriter<StringBuilder>
    {
        private StringBuilder _builder = new StringBuilder();

        public override void StartWritingNode(string name, Guid id)
        {
            _builder.AppendLine(string.Format("Start: {0}, {1}", name, id));
        }

        public override void WriteAttribute(string name, object value)
        {
            _builder.AppendLine(string.Format("Attribute: {0}, {1}", name, value));
        }

        public override void EndWritingNode()
        {
            _builder.AppendLine("End");
        }

        public override StringBuilder Open(string nmspc)
        {
            return _builder;
        }

        public override void Close()
        {
            
        }
    }
}
