using CrewBot.Classes.Abstract;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace CrewBot.Classes.Commands
{
    public class SettingCommand : MessageCommandBase
    {
        public static string[] NormalizeArguments(string content)
        {
            string[] msg = (content ?? string.Empty).ToLower().Split(new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries);
            if (msg.Length == 0)
            {
                return msg;
            }

            switch (msg[0])
            {
                case "+setlogchannel":
                case "!setlogchannel":
                    return new[] { msg[0], "logchannel", "set" };
                case "+removelogchannel":
                case "!removelogchannel":
                    return new[] { msg[0], "logchannel", "unset" };
                case "+setdmchannel":
                case "!setdmchannel":
                    return new[] { msg[0], "dmchannel", "set" };
                case "+removedmchannel":
                case "!removedmchannel":
                    return new[] { msg[0], "dmchannel", "unset" };
                default:
                    return msg;
            }
        }

        public async Task AdminAction(SocketMessage message, BotConfig botConfig, bool owner)
        {
            string[] msg = NormalizeArguments(message.Content);
            if (msg.Length < 2)
            {
                await UserAction(message, botConfig);
                return;
            }

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
                                        BotStartup.SaveConfig(botConfig);
                                        await message.Channel.SendMessageAsync($"Role: {message.MentionedRoles.FirstOrDefault().Name} with ID:{message.MentionedRoles.FirstOrDefault().Id} was set as admin;");
                                    }
                                    else
                                    {
                                        await message.Channel.SendMessageAsync("You must mention exactly one role to assign as admin.");
                                    }
                                    break;
                                case "remove":
                                    botConfig.AdminID = 0;
                                    BotStartup.SaveConfig(botConfig);
                                    await message.Channel.SendMessageAsync($"ADMIN role has been unset");
                                    break;
                            }
                        }
                        break;
                    case "bot":
                        if (msg.Length > 2)
                        {
                            switch (msg[2])
                            {
                                case "enable":
                                    botConfig.BotEnabled = true;
                                    BotStartup.SaveConfig(botConfig);
                                    await message.Channel.SendMessageAsync("Bot runtime is now enabled in config");
                                    break;
                                case "disable":
                                    botConfig.BotEnabled = false;
                                    BotStartup.SaveConfig(botConfig);
                                    await message.Channel.SendMessageAsync("Bot runtime is now disabled in config");
                                    break;
                            }
                        }
                        break;
                }
            }
            else if (msg[1] == "admin")
            {
                await message.Channel.SendMessageAsync("Only the server owner can add or remove the bot admin role.");
                return;
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
                                    BotStartup.SaveConfig(botConfig);
                                    await message.Channel.SendMessageAsync($"Log Channel is now #{message.MentionedChannels.FirstOrDefault().Name}, ID:{message.MentionedChannels.FirstOrDefault().Id}");
                                }
                                break;
                            case "unset":
                                botConfig.LogChannelID = 0;
                                BotStartup.SaveConfig(botConfig);
                                await message.Channel.SendMessageAsync($"Log Channel has been unset");
                                break;
                        }
                    }
                    break;
                case "dmchannel":
                    if (msg.Length > 2)
                    {
                        switch (msg[2])
                        {
                            case "set":
                                if (message.MentionedChannels.Count == 1)
                                {
                                    botConfig.DMChannelID = message.MentionedChannels.FirstOrDefault().Id;
                                    BotStartup.SaveConfig(botConfig);
                                    await message.Channel.SendMessageAsync($"DM Channel is now #{message.MentionedChannels.FirstOrDefault().Name}, ID:{message.MentionedChannels.FirstOrDefault().Id}");
                                }
                                break;
                            case "unset":
                                botConfig.DMChannelID = 0;
                                BotStartup.SaveConfig(botConfig);
                                await message.Channel.SendMessageAsync($"DM Channel has been unset");
                                break;
                        }
                    }
                    break;
                case "colorchannel":
                    if (msg.Length > 2)
                    {
                        switch (msg[2])
                        {
                            case "set":
                                if (message.MentionedChannels.Count == 1)
                                {
                                    botConfig.ColorChannelID = message.MentionedChannels.FirstOrDefault().Id;
                                    BotStartup.SaveConfig(botConfig);
                                    await message.Channel.SendMessageAsync($"Color Channel is now #{message.MentionedChannels.FirstOrDefault().Name}, ID:{message.MentionedChannels.FirstOrDefault().Id}");
                                }
                                break;
                            case "unset":
                                botConfig.ColorChannelID = 0;
                                BotStartup.SaveConfig(botConfig);
                                await message.Channel.SendMessageAsync($"Color Channel has been unset");
                                break;
                        }
                    }
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
                                    BotStartup.SaveConfig(botConfig);
                                    await message.Channel.SendMessageAsync($"General Channel is now #{message.MentionedChannels.FirstOrDefault().Name}, ID:{message.MentionedChannels.FirstOrDefault().Id}");
                                }
                                break;
                            case "unset":
                                botConfig.GeneralChannelID = 0;
                                BotStartup.SaveConfig(botConfig);
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
                                ulong disabledChannelId = message.MentionedChannels.FirstOrDefault().Id;
                                if (!Program.ignoreMessagesCache.Contains(disabledChannelId))
                                {
                                    Program.ignoreMessagesCache.Add(disabledChannelId);
                                    SerializeJsonObject(BotStartup.IgnoreMessageCachePath, Program.ignoreMessagesCache);
                                }

                                await message.Channel.SendMessageAsync($"Message logging in <#{disabledChannelId}> ID:{disabledChannelId} is disabled");
                                break;
                            case "enable":
                                if (Program.ignoreMessagesCache.Contains(message.MentionedChannels.FirstOrDefault().Id))
                                {
                                    Program.ignoreMessagesCache.Remove(message.MentionedChannels.FirstOrDefault().Id);
                                    SerializeJsonObject(BotStartup.IgnoreMessageCachePath, Program.ignoreMessagesCache);
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
            string[] msg = NormalizeArguments(message.Content);
            if (msg.Length < 2)
            {
                return;
            }
            switch (msg[1])
            {
                case "list":
                case "status":
                    await message.Channel.SendMessageAsync(BuildStatusMessage(
                        botConfig,
                        Program.client?.ConnectionState == Discord.ConnectionState.Connected,
                        BotStartup.DataDirectoryPath));
                    break;
                case "help":
                    await message.Channel.SendMessageAsync($"{HelpMessage(botConfig.Prefix)}");
                    break;
            }
        }

        public static string BuildStatusMessage(BotConfig botConfig, bool isConnected, string dataDirectoryPath)
        {
            BotConfig config = botConfig ?? new BotConfig();
            string prefix = string.IsNullOrWhiteSpace(config.Prefix) ? "+" : config.Prefix;
            string dataDirectory = string.IsNullOrWhiteSpace(dataDirectoryPath) ? "(unknown)" : dataDirectoryPath;

            string output = "```";
            output += $"\nStatus";
            output += $"\nConnected: {isConnected}";
            output += $"\nBotEnabled: {config.BotEnabled}";
            output += $"\nPrefix: {prefix}";
            output += $"\nDataDirectory: {dataDirectory}";
            output += $"\nGuildID: {config.GuildID}";
            output += $"\nBotID: {config.BotID}";
            output += $"\nAdminRoleID: {config.AdminID}";
            output += $"\nAdditionalOwnerID: {config.AdditionalOwnerID}";
            output += $"\nLogChannelID: {config.LogChannelID}";
            output += $"\nDMChannelID: {config.DMChannelID}";
            output += $"\nGeneralChannelID: {config.GeneralChannelID}";
            output += $"\nColorChannelID: {config.ColorChannelID}";
            output += $"\nColorRoleID: {config.ColorRoleID}";
            output += $"\nColorEnabled: {config.ColorEnabled}";
            output += $"\nColorAdminEnabled: {config.ColorAdminEnabled}";
            output += $"\nColorTimerSeconds: {config.ColorTimerSeconds}";
            output += $"\nTriggerEnabled: {config.TriggerEnabled}";
            output += $"\nTriggerAdminEnabled: {config.TriggerAdminEnabled}";
            output += $"\nSettingsAdminEnabled: {config.SettingsAdminEnabled}";
            output += "\n```";
            return output;
        }

        private string HelpMessage(string prefix)
        {
            string returnString = $"Owner only comamnds:";
            returnString += "\n - admin\n -- add\n -- remove\n --- adds or removes a role to be given 'admin' on the server with or without administrator permissions";
            returnString += "\nAdmin only commands:";
            returnString += "\n - logchannel\n -- set\n -- unset\n --- Sets or unsets the primary logging channel";
            returnString += "\n - dmchannel\n -- set\n -- unset\n --- Sets or unsets the bot DM relay channel";
            returnString += "\n - colorchannel\n -- set\n -- unset\n --- Sets or unsets the channel referenced in the welcome message";
            returnString += "\n - general\n -- set\n -- unset\n --- Sets or unsets the server's general channel";
            returnString += "\n - messagelogging\n -- enable\n -- disable\n --- Requires a mentioned channel - sets mentioned channel to have message logging enabled or disabled";
            returnString += "\nOwner runtime commands:";
            returnString += "\n - bot\n -- enable\n -- disable\n --- Enables or disables the bot connection from config";
            returnString += "\nEveryone commands:";
            returnString += "\n - status / list\n -- Shows current runtime and config values";
            returnString += "\n - help:\n -- Shows this help command (more to follow)";
            return returnString;
        }
    }
}
