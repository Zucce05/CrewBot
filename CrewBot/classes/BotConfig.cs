using System;
using System.Collections.Generic;
using System.Text;

namespace CrewBot.Classes
{
    public class BotConfig
    {
        // General Settings
        public bool SettingsAdminEnabled { get; set; }
        public string Token { get; set; }
        public ulong GuildID { get; set; }
        public ulong LogChannelID { get; set; }
        public ulong DMChannelID { get; set; }
        public ulong BotID { get; set; }
        public ulong AdminID { get; set; }
        public ulong GeneralChannelID { get; set; }
        public string Prefix { get; set; }

        // Color Settings
        public int ColorTimerSeconds { get; set; }
        public ulong ColorRoleID { get; set; }
        public bool ColorEnabled { get; set; }
        public bool ColorAdminEnabled { get; set; }

        // Trigger Settings
        public bool TriggerEnabled { get; set; }
        public bool TriggerAdminEnabled { get; set; }

    }
}
