using CrewBot.Classes.Abstract;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace CrewBot.Classes.Commands
{
    class SettingCommand : MessageCommandBase
    {
        public async Task AdminAction(SocketMessage message, BotConfig botConfig, bool owner)
        {
            string[] msg = message.Content.ToLower().Split(new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries);

            if (owner)
            {
                switch (msg[1])
                {
                    case "admin":
                        if (msg.Length > 2)
                        {
                            switch (msg[2])
                            {
                                case "add":
                                    if (message.MentionedRoles.Count > 0)
                                    {
                                        botConfig.AdminID = message.MentionedRoles.FirstOrDefault().Id;
                                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                                        await message.Channel.SendMessageAsync($"Role: {message.MentionedRoles.FirstOrDefault().Name} with ID:{message.MentionedRoles.FirstOrDefault().Id} was set as admin;");
                                    }
                                    break;
                                case "remove":
                                    botConfig.AdminID = 0;
                                    SerializeJsonObject($"json/BotConfig.json", botConfig);
                                    await message.Channel.SendMessageAsync($"ADMIN role has been unset");
                                    break;
                            }
                        }
                        break;
                }
            }

            switch (msg[1])
            {
                case "logchannel":
                    if (msg.Length > 2)
                    {
                        switch (msg[2])
                        {
                            case "set":
                                if (message.MentionedChannels.Count == 1)
                                {
                                    botConfig.LogChannelID = message.MentionedChannels.FirstOrDefault().Id;
                                    SerializeJsonObject($"json/BotConfig.json", botConfig);
                                    await message.Channel.SendMessageAsync($"Log Channel is now #{message.MentionedChannels.FirstOrDefault().Name}, ID:{message.MentionedChannels.FirstOrDefault().Id}");
                                }
                                break;
                            case "unset":
                                botConfig.LogChannelID = 0;
                                SerializeJsonObject($"json/BotConfig.json", botConfig);
                                await message.Channel.SendMessageAsync($"Log Channel has been unset");
                                break;
                        }
                    }
                    break;
                case "color":
                    break;
                case "general":
                    break;
                default:
                    await UserAction(message, botConfig);
                    break;
            }

        }
        public async Task UserAction(SocketMessage message, BotConfig botConfig)
        {
            string[] msg = message.Content.ToLower().Split(new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries);
            switch (msg[1])
            {
                case "list":
                    break;
                case "help":
                    await message.Channel.SendMessageAsync($"{HelpMessage(botConfig.Prefix)}");
                    break;
            }
        }

        private string HelpMessage(string prefix)
        {
            string returnString = $"There is no help message created here yet. Let Zucce know.";
            return returnString;
        }
    }
}
