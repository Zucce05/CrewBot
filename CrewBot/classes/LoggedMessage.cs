using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrewBot.Classes
{
    public class LoggedMessage
    {
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

        public ulong userID { get; private set; }
        public ulong messageID { get; private set; }
        public string messageContent { get; private set; }
        public DateTimeOffset messageDateTimeOffset { get; private set; }
        public string messageAvatarURL { get; private set; }
        public string messageAuthorUsername { get; private set; }
        public string messageChannelName { get; private set; }
        public ulong messageChannelID { get; private set; }
    }
}
