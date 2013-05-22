using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Dataspace.Common.Projections.Classes;

namespace Dataspace.Common.Projections.Services
{

    [Export(typeof(ProjectionWriter<Stream>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class XmlWriter:ProjectionWriter<Stream>
    {
        [ThreadStatic]
        private static MemoryStream _stream;

        [ThreadStatic]
        private static System.Xml.XmlWriter _writer;

        private string _namespace;


        public override void StartWritingNode(string name, Guid id)
        {
            _writer.WriteStartElement(name,_namespace);
            _writer.WriteAttributeString(FilledProjectionFrame.IdName,id.ToString().ToUpper());
        }

        public override void WriteAttribute(string name, object value)
        {
            _writer.WriteAttributeString(name,  value == null?"{x:Null}":value.ToString());
        }

        public override void EndWritingNode()
        {
            _writer.WriteEndElement();
        }

        public override Stream Open(string nmspc)
        {
           
            _namespace = nmspc;
            _stream = new MemoryStream();
            _writer = System.Xml.XmlWriter.Create(_stream);
            _writer.WriteStartDocument();          
            return _stream;
        }

        public override void Close()
        {
            _writer.WriteEndDocument();
            _writer.Close();                
        }
    }
}
