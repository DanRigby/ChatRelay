using ChatRelay.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ChatRelay
{
    public static class ConfigurationManager
    {
        private const string ConfigFileName = @"ChatRelayConfig.json";

        public static Dictionary<string, ChatServer> ChatServers = new Dictionary<string, ChatServer>();
        public static List<ChannelMapping> ChannelMappings = new List<ChannelMapping>();

        public static void ReloadConfiguration()
        {
            ChatServers.Clear();
            ChannelMappings.Clear();

            if (!File.Exists(ConfigFileName))
            {
                throw new RelayConfigurationException(
                    $"Could not locate the configuration file '{ConfigFileName}' in the executing directory.");
            }

            string jsonText = File.ReadAllText(ConfigFileName);
            RelayConfiguration relayConfig = JsonConvert.DeserializeObject<RelayConfiguration>(jsonText);

            foreach (ChatServer chatServer in relayConfig.ChatServers)
            {
                if (ChatServers.ContainsKey(chatServer.ServerId))
                {
                    throw new RelayConfigurationException(
                        $"Relay configuration contains duplicate server id '{chatServer.ServerId}'.");
                }

                ChatServers.Add(chatServer.ServerId, chatServer);
            }

            foreach (ChannelMapping channelMapping in relayConfig.ChannelMappings)
            {
                bool mappingExists = ChannelMappings.Exists(x =>
                    x.SourceServerId == channelMapping.SourceServerId &&
                    x.SourceChannel == channelMapping.SourceChannel &&
                    x.TargetServerId == channelMapping.TargetServerId &&
                    x.TargetChannel == channelMapping.TargetChannel);
                if (mappingExists)
                {
                    throw new RelayConfigurationException(
                        "Relay configuration contains a duplicate channel mapping.");
                }

                bool selfMap =
                    channelMapping.SourceServerId == channelMapping.TargetServerId &&
                    channelMapping.SourceChannel == channelMapping.TargetChannel;
                if (selfMap)
                {
                    throw new RelayConfigurationException(
                        "Relay configuration contains a mapping where the source and target are the same.");
                }

                ChannelMappings.Add(channelMapping);
            }
        }
    }

    public class RelayConfigurationException : Exception
    {
        public RelayConfigurationException()
        { }

        public RelayConfigurationException(string message)
        : base(message)
        { }

        public RelayConfigurationException(string message, Exception innerException)
        : base(message, innerException)
        { }
    }
}
