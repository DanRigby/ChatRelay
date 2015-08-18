using ChatRelay.Models;
using JabbR.Client;
using JabbR.Client.Models;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ChatRelay.Adapters
{
    public class JabbrAdapter : IChatServerAdapter
    {
        private readonly JabbRClient client;
        private readonly ChatServer serverConfig;

        public IObservable<ChatMessage> Messages { get; private set; }

        public JabbrAdapter(ChatServer chatServerConfig)
        {
            serverConfig = chatServerConfig;
            client = new JabbRClient(serverConfig.ServerAddress)
            {
                AutoReconnect = true
            };

            Messages = Observable.FromEvent<Action<Message, string>, ChatMessage>(handler =>
            {
                Action<Message, string> converter = (jabbrMessage, room) =>
                {
                    try
                    {
                        if (client == null)
                        {
                            return;
                        }

                        // Don't relay our own messages
                        if (jabbrMessage.User.Name.Equals(serverConfig.UserName, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        var chatMessage = new ChatMessage
                        {
                            ServerId = serverConfig.ServerId,
                            Room = room,
                            User = jabbrMessage.User.Name,
                            Text = jabbrMessage.Content,
                            TimeStamp = jabbrMessage.When
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
                converter => client.MessageReceived += converter,
                converter => client.MessageReceived -= converter
            );
        }

        public async Task Connect()
        {
            await client.Connect(serverConfig.UserName, serverConfig.Password);
            Console.WriteLine("Connected to {0}.", serverConfig.ServerId);
        }

        public void Disconnect()
        {
            Messages = null;
            client?.Disconnect();

            Console.WriteLine("Disconnected from {0}.", serverConfig.ServerId);
        }

        public async void JoinChannel(string channelName)
        {
            await client.JoinRoom(channelName);
        }

        public async void SendMessage(string destinationRoom, ChatMessage message)
        {
            try
            {
                string messageText = $"**{message.User}[{message.ServerId}]**: {message.Text}";
                await client.Send(messageText, destinationRoom);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{serverConfig.ServerId}|EXCEPTION: {ex}");
            }
        }
    }
}
