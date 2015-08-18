using System;

namespace ChatRelay.Models
{
    public class ChatMessage
    {
        public string Text { get; set; }

        public string Room { get; set; }

        public string User { get; set; }

        public string ServerId { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
