using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ChatRelay.Models
{
    /// <summary>
    /// Represents the configuration information necessary to connect to a chat server.
    /// </summary>
    public class ChatServer
    {
        /// <summary>
        /// A unique name for this chat server.
        /// </summary>
        public string ServerId { get; set; }

        /// <summary>
        /// The type of the chat server to connect to.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ChatServerType ServerType { get; set; }

        /// <summary>
        /// The address of the chat server to connect to.
        /// This value is ignored for Slack connections.
        /// </summary>
        public string ServerAddress { get; set; }

        /// <summary>
        /// The user name to use for the chat server.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The password or API key for the specified account (if applicable).
        /// </summary>
        public string Password { get; set; }
    }
}
