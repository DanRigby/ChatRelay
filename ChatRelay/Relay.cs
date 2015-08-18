using ChatRelay.Adapters;
using ChatRelay.Models;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ChatRelay
{
    public class Relay
    {
        private readonly Dictionary<string, IChatServerAdapter> adapters = new Dictionary<string, IChatServerAdapter>();
        private readonly List<IDisposable> subscriptions = new List<IDisposable>();

        public void Start()
        {
            ConfigurationManager.ReloadConfiguration();

            foreach (ChatServer chatServer in ConfigurationManager.ChatServers.Values)
            {
                switch (chatServer.ServerType)
                {
                    case ChatServerType.Slack:
                        adapters.Add(chatServer.ServerId, new SlackAdapter(chatServer));
                        break;
                    case ChatServerType.Jabbr:
                        adapters.Add(chatServer.ServerId, new JabbrAdapter(chatServer));
                        break;
                    case ChatServerType.Irc:
                        adapters.Add(chatServer.ServerId, new IrcAdapter(chatServer));
                        break;
                }
            }

            var connectionTasks = new List<Task>();
            foreach (IChatServerAdapter chatServer in adapters.Values)
            {
                connectionTasks.Add(chatServer.Connect());
            }
            Task.WhenAll(connectionTasks).Wait();

            foreach (IChatServerAdapter chatServerAdapter in adapters.Values)
            {
                subscriptions.Add(
                    chatServerAdapter.Messages
                    .Subscribe(msg => Console.WriteLine($"({msg.ServerId})#{msg.Room} {msg.User}: {msg.Text}"))
                );
            }

            foreach (ChannelMapping channelMapping in ConfigurationManager.ChannelMappings)
            {
                var sourceAdapter = adapters[channelMapping.SourceServerId];
                var targetAdapter = adapters[channelMapping.TargetServerId];

                string sourceChannel = channelMapping.SourceChannel;
                string destinationChannel = channelMapping.TargetChannel;

                sourceAdapter.JoinChannel(sourceChannel);
                targetAdapter.JoinChannel(destinationChannel);

                subscriptions.Add(
                    sourceAdapter.Messages
                    .Where(x => x.Room == sourceChannel)
                    .Subscribe(msg => targetAdapter.SendMessage(destinationChannel, msg))
                );
            }
        }

        public void Stop()
        {
            foreach (IDisposable subscription in subscriptions)
            {
                subscription?.Dispose();
            }
            subscriptions.Clear();

            foreach (IChatServerAdapter chatServerAdapter in adapters.Values)
            {
                chatServerAdapter?.Disconnect();
            }
            adapters.Clear();
        }
    }
}
