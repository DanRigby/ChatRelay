using ChatRelay.Models;
using System;
using System.Threading.Tasks;

namespace ChatRelay.Adapters
{
    public interface IChatServerAdapter
    {
        Task Connect();

        void Disconnect();

        void SendMessage(string destinationRoom, ChatMessage message);

        void JoinChannel(string channelName);

        IObservable<ChatMessage> Messages { get; }
    }
}
