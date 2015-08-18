namespace ChatRelay.Models
{
    /// <summary>
    /// The supported types of Chat Servers.
    /// </summary>
    public enum ChatServerType
    {
        /// <summary>
        /// Unknown type.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A Slack server.
        /// https://slack.com
        /// </summary>
        Slack = 1,

        /// <summary>
        /// A JabbR server.
        /// https://jabbr.net & https://github.com/JabbR/JabbR
        /// </summary>
        Jabbr = 2,

        /// <summary>
        /// An Internet Relay Chat server.
        /// http://www.irchelp.org/irchelp/networks/popular.html
        /// </summary>
        Irc = 3
    }
}
