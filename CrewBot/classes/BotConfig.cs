using System;
using System.Collections.Generic;
using System.Text;

namespace CrewBot.classes
{
    public class BotConfig
    {
        public string Token { get; set; }
        public ulong GuildID { get; set; }
        public ulong LogChannelID { get; set; }
        public ulong DMChannelID { get; set; }
        public ulong BotID { get; set; }
        public ulong ColorRoleID { get; set; }
        public List<ulong> AdminID { get; set; }
    }
}
