using ChatRelay.Models;
using IrcDotNet;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ChatRelay.Adapters
{
    public class IrcAdapter : IChatServerAdapter
    {
        private readonly StandardIrcClient client;
        private readonly ChatServer serverConfig;

        // TODO: I'm not a fan of this approach.
        // Need to see if there is a better way to turn the connection event pattern into a Task.
        private TaskCompletionSource<object> connectingTaskCompletionSource;

        public IObservable<ChatMessage> Messages { get; private set; }

        public IrcAdapter(ChatServer chatServerConfig)
        {
            serverConfig = chatServerConfig;
            client = new StandardIrcClient { FloodPreventer = new IrcStandardFloodPreventer(5, 2000) };

            Messages = Observable.FromEvent<EventHandler<IrcRawMessageEventArgs>, ChatMessage>(handler =>
            {
                EventHandler<IrcRawMessageEventArgs> converter = (sender, ircMessageArgs) =>
                {
                    try
                    {
                        if (client == null)
                        {
                            return;
                        }

                        // If it's not an actual user message, ignore it
                        if (!ircMessageArgs.Message.Command.Equals("PRIVMSG", StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        string messageSender = ircMessageArgs.Message.Source.Name;
                        string message = ircMessageArgs.Message.Parameters[1];
                        string roomName = ircMessageArgs.Message.Parameters[0];
                        if (roomName != null && roomName.StartsWith("#"))
                        {
                            roomName = roomName.Substring(1);
                        }

                        // Don't relay our own messages
                        if (messageSender.Equals(serverConfig.UserName, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        // Check for emotes, ignore for now
                        if (message.StartsWith("\u0001" + "ACTION", StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        var chatMessage = new ChatMessage
                        {
                            ServerId = serverConfig.ServerId,
                            Room = roomName,
                            User = messageSender,
                            Text = message,
                            TimeStamp = DateTimeOffset.Now // IRC Doesn't hand us the timestamp
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
                converter => client.RawMessageReceived += converter,
                converter => client.RawMessageReceived -= converter
            );

            client.Connected += Client_Connected;
            client.Disconnected += Client_Disconnected;

            // Debugging
            client.ConnectFailed += Client_ConnectFailed;
            client.ErrorMessageReceived += Client_ErrorMessageReceived;
        }

        public Task Connect()
        {
            connectingTaskCompletionSource = new TaskCompletionSource<object>();
            client.Connected += OnConnectedCallback;
            client.ConnectFailed += OnConnectFailedCallback;

            var registrationInfo = new IrcUserRegistrationInfo
            {
                NickName = serverConfig.UserName,
                Password = serverConfig.Password,
                RealName = "Relays messages between chat rooms on different services.",
                UserName = serverConfig.UserName
            };
            client.Connect(serverConfig.ServerAddress, false, registrationInfo);

            return connectingTaskCompletionSource.Task;
        }

        private void OnConnectedCallback(object sender, EventArgs eventArgs)
        {
            client.Connected -= OnConnectedCallback;
            connectingTaskCompletionSource.TrySetResult(null);
            connectingTaskCompletionSource = null;
        }

        private void OnConnectFailedCallback(object sender, EventArgs eventArgs)
        {
            client.ConnectFailed -= OnConnectFailedCallback;
            connectingTaskCompletionSource.TrySetException(
                new ApplicationException($"Unable to connect to {serverConfig.ServerId}. Bad configuration?"));
            connectingTaskCompletionSource = null;
        }

        public void Disconnect()
        {
            client.Connected -= Client_Connected;
            client.Disconnected -= Client_Disconnected;
            client.Disconnected -= OnConnectedCallback;
            client.ConnectFailed -= Client_ConnectFailed;
            client.ConnectFailed -= OnConnectFailedCallback;
            client.ErrorMessageReceived -= Client_ErrorMessageReceived;

            Messages = null;
            client?.Disconnect();

            Console.WriteLine($"Disconnected from {serverConfig.ServerId}.");
        }

        public void JoinChannel(string channelName)
        {
            client.Channels.Join("#" + channelName);
        }

        public void SendMessage(string destinationRoom, ChatMessage message)
        {
            string room = "#" + destinationRoom;
            try
            {
                string[] textLines = message.Text.Split('\n');
                if (textLines.Length <= 1)
                {
                    string messageText = $"{message.User}[{message.ServerId}]: {message.Text}";
                    client.SendRawMessage($"PRIVMSG {room} :{messageText}");
                }
                else
                {
                    // Handle multiline messages
                    string hasteBinUrl = CreateHastebinFromText(message.Text);
                    client.SendRawMessage(
                        $"PRIVMSG {room} :{message.User}[{message.ServerId}]: {textLines[0]}");
                    client.SendRawMessage(
                        $"PRIVMSG {room} :[Flood Prevention]: Multiline message from <{message.User}[{message.ServerId}]> truncated. Full message viewable here: {hasteBinUrl}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{serverConfig.ServerId}|EXCEPTION: {ex}");
            }
        }

        private void Client_Connected(object sender, EventArgs e)
        {
            Console.WriteLine("Connected to {0}.", serverConfig.ServerId);
        }

        private void Client_ConnectFailed(object sender, IrcErrorEventArgs e)
        {
            Console.WriteLine($"{serverConfig.ServerId}|CONNECT FAILED: {e.Error}");
        }

        private async void Client_Disconnected(object sender, EventArgs e)
        {
            Console.WriteLine($"Disconnected from {serverConfig.ServerId}.");

            // Attempt to reconnect after 15 seconds
            await Task.Delay(TimeSpan.FromSeconds(15));
            await Connect();
        }

        private void Client_ErrorMessageReceived(object sender, IrcErrorMessageEventArgs e)
        {
            Console.WriteLine($"{serverConfig.ServerId}|ERROR: {e.Message}");
        }

        private string CreateHastebinFromText(string text)
        {
            var httpClient = new HttpClient();
            var content = new StringContent(text, System.Text.Encoding.UTF8, "text/plain");
            HttpResponseMessage response = httpClient.PostAsync("http://hastebin.com/documents", content).Result;
            string resultContent = response.Content.ReadAsStringAsync().Result;
            var jsonObj = JsonConvert.DeserializeAnonymousType(resultContent, new { key = string.Empty });
            string hasteBinUrl = $"http://hastebin.com/{jsonObj.key}";

            return hasteBinUrl;
        }
    }
}
