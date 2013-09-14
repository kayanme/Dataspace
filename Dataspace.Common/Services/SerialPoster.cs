using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Statistics;
using Dataspace.Common.Transactions;

namespace Dataspace.Common.Services
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SerialPoster
    {
       
          private readonly IEnumerable<IPacketResourcePoster> _packetResourcePosters;

        private readonly IStatChannel _statChannel;

        private void EmulatedSerialPost(IEnumerable<DataRecord> resources)
        {
            foreach (var resource in resources)
            {               
                    resource.Poster(resource.Content.ResourceKey,resource.Resource);               
            }
        }

        public void PostPacket(IEnumerable<DataRecord> resources)
        {
            Debug.Assert(resources != null,"resource!=null");
            IEnumerable<DataRecord> notWrittenResources = 
            _packetResourcePosters.
                Aggregate(resources,
                          (a, s) =>
                              {
                                  if (a.Any())
                                  {
                                      var w = Stopwatch.StartNew();
                                      a = s.PostResourceBlock(a).ToArray();
                                      w.Stop();
                                      _statChannel.SendMessageAboutMultipleResources(new Guid[3], Actions.Posted,
                                                                                     w.Elapsed);
                                  }
                                  return a;
                              });
        
            if (notWrittenResources!=null && notWrittenResources.Any())
                EmulatedSerialPost(notWrittenResources);
            
        }

        [ImportingConstructor]
        public SerialPoster([ImportMany]IEnumerable<IPacketResourcePoster> packetResourcePosters,
                                        IStatChannel statChannel,
                                        SettingsHolder settings,
                                        AppConfigProvider appConfigProvider
            )
        {
            _packetResourcePosters = packetResourcePosters
                .Where(k => settings.Settings
                                    .ActivationSwitchMatch(k.GetType(), appConfigProvider));
            _statChannel = statChannel;
            _statChannel.Register("Universal");
        }
    }
}
