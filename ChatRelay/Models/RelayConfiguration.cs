using System.Collections.Generic;

namespace ChatRelay.Models
{
    public class RelayConfiguration
    {
        public List<ChatServer> ChatServers { get; set; }
        public List<ChannelMapping> ChannelMappings { get; set; }
    }
}
