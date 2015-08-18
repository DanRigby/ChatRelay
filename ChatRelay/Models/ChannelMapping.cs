namespace ChatRelay.Models
{
    /// <summary>
    /// Represents a single one way chat rely mapping from one a channel on a server to another channel on a server.
    /// </summary>
    public class ChannelMapping
    {
        /// <summary>
        /// The id of the server to relay messages from.
        /// </summary>
        public string SourceServerId { get; set; }

        /// <summary>
        /// The name of the channel or room on the source server to relay messages from.
        /// </summary>
        public string SourceChannel { get; set; }

        /// <summary>
        /// The id of the server to relay messages to.
        /// </summary>
        public string TargetServerId { get; set; }

        /// <summary>
        /// The name of the channel or room on the source server to relay messages to.
        /// </summary>
        public string TargetChannel { get; set; }
    }
}
