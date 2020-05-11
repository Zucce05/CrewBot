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
                // general is the "general" channel on the server. Used to post 'everyone' messages like user joined/left, etc.
                case "general":
                    if (msg.Length > 2)
                    {
                        switch (msg[2])
                        {
                            case "set":
                                if (message.MentionedChannels.Count == 1)
                                {
                                    botConfig.GeneralChannelID = message.MentionedChannels.FirstOrDefault().Id;
                                    SerializeJsonObject($"json/BotConfig.json", botConfig);
                                    await message.Channel.SendMessageAsync($"General Channel is now #{message.MentionedChannels.FirstOrDefault().Name}, ID:{message.MentionedChannels.FirstOrDefault().Id}");
                                }
                                break;
                            case "unset":
                                botConfig.LogChannelID = 0;
                                SerializeJsonObject($"json/BotConfig.json", botConfig);
                                await message.Channel.SendMessageAsync($"General Channel has been unset");
                                break;
                        }
                    }
                    break;
                // messageLogging is for the logging feature to turn the default delete logging (and others eventually) on and off.
                // void channels should be ignored. This is a consistency improvement allowing dynamic adding and removing of void channels.
                // Can be used to ignore other channels as needed (admin/mod/etc?)
                case "messagelogging":
                    if (message.MentionedChannels.Count > 0 && msg.Length > 2)
                    {
                        switch (msg[2])
                        {
                            case "disable":
                                Program.ignoreMessagesCache.Add(message.MentionedChannels.FirstOrDefault().Id);
                                SerializeJsonObject($"json/ignoreMessageCache.json", Program.ignoreMessagesCache);
                                await message.Channel.SendMessageAsync($"Message logging in <#{message.MentionedChannels.FirstOrDefault().Id}> ID:{message.MentionedChannels.FirstOrDefault().Id} is disabled");
                                break;
                            case "enable":
                                if (Program.ignoreMessagesCache.Contains(message.MentionedChannels.FirstOrDefault().Id))
                                {
                                    Program.ignoreMessagesCache.Remove(message.MentionedChannels.FirstOrDefault().Id);
                                    SerializeJsonObject($"json/ignoreMessageCache.json", Program.ignoreMessagesCache);
                                    await message.Channel.SendMessageAsync($"Message logging in #<{message.MentionedChannels.FirstOrDefault().Id}> ID:{message.MentionedChannels.FirstOrDefault().Id} is enabled");
                                }
                                break;
                        }
                    }
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
            string returnString = $"Owner only comamnds:";
            returnString += "\n - admin\n -- add\n -- remove\n --- adds or removes a role to be given 'admin' on the server with or without administrator permissions";
            returnString += "\nAdmin only commands:";
            returnString += "\n - logchannel\n -- set\n -- unset\n --- Sets or unsets the primary logging channel";
            returnString += "\n - general\n -- set\n -- unset\n --- Sets or unsets the server's general channel";
            returnString += "\n - messagelogging\n -- enable\n -- disable\n --- Requires a mentioned channel - sets mentioned channel to have message logging enabled or disabled";
            returnString += "\nEveryone commands:";
            returnString += "\n - help:\n -- Shows this help command (more to follow)";
            return returnString;
        }
    }
}
