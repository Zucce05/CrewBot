using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrewBot.Classes
{
    public class LoggedMessage
    {
        public LoggedMessage() { }
        public LoggedMessage(SocketMessage message)
        {
            userID = message.Author.Id;
            messageID = message.Id;
            messageContent = message.Content;
            messageDateTimeOffset = message.CreatedAt;
            messageAvatarURL = message.Author.GetAvatarUrl();
            messageAuthorUsername = message.Author.Username;
            messageChannelName = message.Channel.Name;
            messageChannelID = message.Channel.Id;
        }

        public ulong userID { get; set; }
        public ulong messageID { get; set; }
        public string messageContent { get; set; }
        public DateTimeOffset messageDateTimeOffset { get; set; }
        public string messageAvatarURL { get; set; }
        public string messageAuthorUsername { get; set; }
        public string messageChannelName { get; set; }
        public ulong messageChannelID { get; set; }
    }
}
