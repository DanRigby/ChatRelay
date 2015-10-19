using ChatRelay.Models;
using SlackAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace ChatRelay.Adapters
{
    public class SlackAdapter : IChatServerAdapter
    {
        private readonly SlackSocketClient client;
        private readonly ChatServer serverConfig;

        private Dictionary<string, Channel> channelNameLookup;
        private readonly Timer connectionStatusTimer;

        public IObservable<ChatMessage> Messages { get; private set; }

        public SlackAdapter(ChatServer chatServerConfig)
        {
            serverConfig = chatServerConfig;
            client = new SlackSocketClient(serverConfig.Password);

            Messages = Observable.FromEvent<Action<SlackAPI.WebSocketMessages.NewMessage>, ChatMessage>(handler =>
            {
                Action<SlackAPI.WebSocketMessages.NewMessage> converter = slackMessage =>
                {
                    try
                    {
                        if (client == null || string.IsNullOrWhiteSpace(slackMessage.user))
                        {
                            return;
                        }

                        User user = client.UserLookup[slackMessage.user];

                        // Don't relay our own messages
                        if (user.name.Equals(serverConfig.UserName, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        if (slackMessage.channel.StartsWith("D"))
                        {
                            Console.WriteLine("({0})DirectMessage {1}: {2}", serverConfig.ServerId, user.name, slackMessage.text);
                            return;
                        }

                        Channel channel = slackMessage.channel.StartsWith("C")
                            ? client.ChannelLookup[slackMessage.channel]
                            : client.GroupLookup[slackMessage.channel];

                        var chatMessage = new ChatMessage
                        {
                            ServerId = serverConfig.ServerId,
                            Room = channel.name,
                            User = user.name,
                            Text = FormatIncomingMessage(slackMessage.text),
                            TimeStamp = slackMessage.ts
                        };

                        handler(chatMessage);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{serverConfig.ServerId}|EXCEPTION: {ex}");
                    }
                };

                return converter;
            },
                converter => client.OnMessageReceived += converter,
                converter => client.OnMessageReceived -= converter
            );

            connectionStatusTimer = new Timer(15000) { AutoReset = true };
            connectionStatusTimer.Elapsed += ConnectionStatusTimer_Elapsed;
        }

        public Task Connect()
        {
            var connectingTaskCompletionSource = new TaskCompletionSource<object>();
            client.Connect(o => { }, () =>
            {
                Console.WriteLine("Connected to {0}.", serverConfig.ServerId);
                channelNameLookup = client.Channels.Union(client.Groups).ToDictionary(x => x.name);
                connectionStatusTimer.Start();
                connectingTaskCompletionSource.TrySetResult(null);
            });

            return connectingTaskCompletionSource.Task;
        }

        public void Disconnect()
        {
            Messages = null;
            connectionStatusTimer.Elapsed -= ConnectionStatusTimer_Elapsed;
            connectionStatusTimer.Dispose();
            channelNameLookup = null;
            client?.CloseSocket();

            Console.WriteLine("Disconnected from {0}.", serverConfig.ServerId);
        }

        public void JoinChannel(string channelName)
        {
            // Do nothing. Bots must be invited to channels on Slack, they can't join on their own.
        }

        public void SendMessage(string destinationRoom, ChatMessage message)
        {
            try
            {
                string messageText = $"*{message.User}[{message.ServerId}]*: {message.Text}";
                string formattedMessage = FormatOutgoingMessage(messageText);
                string channelId = channelNameLookup[destinationRoom].id;
                client.SendMessage(null, channelId, formattedMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{serverConfig.ServerId}|EXCEPTION: {ex}");
            }
        }

        private void ConnectionStatusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!client.IsConnected)
            {
                connectionStatusTimer.Stop();
                Console.WriteLine("Disconnected from {0}.", serverConfig.ServerId);

                Connect();
            }
        }

        private string FormatIncomingMessage(string message)
        {
            // https://api.slack.com/docs/formatting
            string formattedMessage = Regex.Replace(message, @"<(.*?)>", match =>
            {
                string matchText = match.ToString().Replace("<", string.Empty).Replace(">", string.Empty);
                string[] splitText = matchText.Split('|');

                if (matchText.StartsWith("@"))
                {
                    return "@" + client.UserLookup[splitText[0].Substring(1)].name;
                }

                if (matchText.StartsWith("#"))
                {
                    return "#" + client.ChannelLookup[splitText[0].Substring(1)].name;
                }

                if (matchText.StartsWith("!"))
                {
                    return string.Empty;
                }

                // For URLs, show what the user entered, not what the server resolved it to
                return splitText.Length > 1 && !string.IsNullOrWhiteSpace(splitText[1]) ? splitText[1] : splitText[0];
            });

            // Remove any character encoding
            formattedMessage = formattedMessage
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
            // Replace ``` characters. JabbR supports them for single lines only & IRC not at all.
                .Replace(" ```", string.Empty)
                .Replace("``` ", string.Empty)
                .Replace("```", string.Empty); // Todo: This should probably be a regex...  http://xkcd.com/1171/

            // TODO: I need to come up with a way to markup the text in a neutral way and then have each adapter
            // decide how to handle things like "```" and "**". Maybe I just convert to markdown [CommonMark]? 

            return formattedMessage;
        }

        private string FormatOutgoingMessage(string message)
        {
            // https://api.slack.com/docs/formatting
            string formattedMessage = message
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

            return formattedMessage;
        }
    }
}
