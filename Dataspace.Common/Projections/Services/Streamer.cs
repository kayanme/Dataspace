using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using Dataspace.Common.Projections.Classes;
using Dataspace.Common.Projections.Storages;

namespace Dataspace.Common.Projections.Services
{

   
    internal class Streamer<TStream> 
    {

        private ProjectionWriter<TStream> _writer;

        private void StreamFilledFrame(FilledProjectionFrame filledFrame)
        {
            _writer.StartWritingNode(filledFrame.Name, filledFrame.Key);
            foreach(var key in filledFrame.Keys.Where(k=>k!="Id" && k!=RegistrationStorage.DefinitlyNoChildren))
                _writer.WriteAttribute(key,filledFrame[key]);
            if (filledFrame.ChildFrames.Any())
            {
                foreach (var child in filledFrame.ChildFrames)
                    StreamFilledFrame(child);
            }
            else
            {
                if ((bool)filledFrame[RegistrationStorage.DefinitlyNoChildren])
                   _writer.WriteAttribute(RegistrationStorage.DefinitlyNoChildren, true);                
            }

            _writer.EndWritingNode();
        }

        public TStream StreamFilledFrames(FilledProjectionFrame filledFrame, string nmspc)
        {
            var stream = _writer.Open(nmspc);
            StreamFilledFrame(filledFrame);
            _writer.Close();
            return stream;
        }

        [ImportingConstructor]
        public Streamer(ProjectionWriter<TStream> writer)
        {
            _writer = writer;
        }
    }
}
