using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Transactions;

namespace Dataspace.Common.Services
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SerialPoster
    {
       
        private readonly IPacketResourcePoster _packetResourcePoster;

        private void EmulatedSerialPost(IEnumerable<DataRecord> resources)
        {
            foreach (var resource in resources)
            {               
                    resource.Poster(resource.Content.ResourceKey,resource.Resource);               
            }
        }

        public void PostPacket(IEnumerable<DataRecord> resources)
        {
            IEnumerable<DataRecord> notWrittenResources = resources;
            if (_packetResourcePoster != null)
                notWrittenResources = _packetResourcePoster.PostResourceBlock(notWrittenResources);
            if (notWrittenResources!=null&& notWrittenResources.Any())
                EmulatedSerialPost(notWrittenResources);
            
        }

        [ImportingConstructor]
        public SerialPoster([Import(AllowDefault = true)]IPacketResourcePoster packetResourcePoster)
        {
            _packetResourcePoster = packetResourcePoster;
        }
    }
}
