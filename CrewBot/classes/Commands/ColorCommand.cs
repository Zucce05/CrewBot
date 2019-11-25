using CrewBot.Classes.Abstract;
using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CrewBot.Classes.Commands
{
    class ColorCommand : MessageCommandBase
    {
        public async Task AdminAction(SocketMessage message, ConcurrentDictionary<ulong, string> colorChoices, string prefix, BotConfig botConfig, bool owner)
        {
            string[] msg = message.Content.ToLower().Split(new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries);
            if (msg.Length > 1)
            {
                if (owner)
                {
                    switch (msg[1])
                    {
                        case "admin":
                            if (msg.Length > 2)
                            {
                                switch (msg[2])
                                {
                                    case "enable":
                                        botConfig.ColorAdminEnabled = true;
                                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                                        await message.Channel.SendMessageAsync($"Administrator use for ``color`` commands is now enabled");
                                        break;
                                    case "disable":
                                        botConfig.ColorAdminEnabled = false;
                                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                                        await message.Channel.SendMessageAsync($"Administrator use for ``color`` commands is now disabled");
                                        break;
                                    case "toggle":
                                        botConfig.ColorAdminEnabled = !botConfig.ColorAdminEnabled;
                                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                                        if (botConfig.ColorAdminEnabled)
                                        {
                                            await message.Channel.SendMessageAsync($"Administrator use for ``color`` commands is now enabled");
                                        }
                                        else
                                        {
                                            await message.Channel.SendMessageAsync($"Administrator use for ``color`` commands is now disabled");
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }

                if (botConfig.ColorAdminEnabled || owner)
                {
                    switch (msg[1])
                    {
                        case "module":
                            if (msg.Length > 2)
                            {
                                switch (msg[2])
                                {
                                    case "enable":
                                        botConfig.ColorEnabled = true;
                                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                                        await message.Channel.SendMessageAsync($"Module ``color`` is now enabled");
                                        break;
                                    case "disable":
                                        botConfig.ColorEnabled = false;
                                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                                        await message.Channel.SendMessageAsync($"Module ``color`` is now disabled");
                                        break;
                                    case "toggle":
                                        botConfig.ColorEnabled = !botConfig.ColorEnabled;
                                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                                        if (botConfig.ColorEnabled)
                                        {
                                            await message.Channel.SendMessageAsync($"Module ``color`` is now enabled");
                                        }
                                        else
                                        {
                                            await message.Channel.SendMessageAsync($"Module ``color`` is now disabled");
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
                if (botConfig.ColorEnabled)
                {
                    switch (msg[1])
                    {
                        case "add":
                            _ = Program.Log(new LogMessage(LogSeverity.Verbose, $"Program", $"MessageRecieved :: +addcolors"));
                            foreach (var color in message.MentionedRoles)
                            {
                                colorChoices.TryAdd(color.Id, color.Name);
                                //colorRoles.Add(color);
                            }
                            await message.Channel.SendMessageAsync($"Color ``{message.MentionedRoles.FirstOrDefault().Name}`` added");
                            SerializeJsonObject($"json/ColorChoices.json", colorChoices);
                            break;
                        case "remove":
                            _ = Program.Log(new LogMessage(LogSeverity.Verbose, $"Program", $"MessageRecieved :: +removecolors"));
                            foreach (var color in message.MentionedRoles)
                            {
                                colorChoices.TryRemove(color.Id, out string value);
                                //colorRoles.Remove(color);
                            }
                            await message.Channel.SendMessageAsync($"Color ``{message.MentionedRoles.FirstOrDefault().Name}`` removed");
                            SerializeJsonObject($"json/ColorChoices.json", colorChoices);
                            break;
                        case "set":
                            if (message.MentionedRoles.Count == 1)
                            {
                                botConfig.ColorRoleID = message.MentionedRoles.FirstOrDefault().Id;
                                SerializeJsonObject($"json/BotConfig.json", botConfig);
                            }
                            else
                            {
                                await message.Channel.SendMessageAsync($"You must mention exactly one role for this command\nExample: ``{prefix}color set @chameleon``");
                            }
                            break;
                        case "unset":
                            botConfig.ColorRoleID = 0;
                            SerializeJsonObject($"json/BotConfig.json", botConfig);
                            break;
                        default:
                            await UserAction(message, colorChoices, prefix);
                            break;
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync($"This module (``color``) is disabled");
                }
            }
        }

        public async Task UserAction(SocketMessage message, ConcurrentDictionary<ulong, string> colorChoices, string prefix)
        {
            string[] msg = message.Content.ToLower().Split(new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries);
            switch (msg[1])
            {

                case "list":
                    await message.Channel.SendMessageAsync($"{ListCommand(colorChoices)}");
                    break;
                case "help":
                    await message.Channel.SendMessageAsync($"{HelpMessage(prefix)}");
                    break;
            }
        }

        private string ListCommand(ConcurrentDictionary<ulong, string> colorChoices)
        {
            string output = $"```\nCurrent Color Roles:\n";
            foreach (KeyValuePair<ulong, string> role in colorChoices)
            {
                output += $"Name: {role.Value} \tID:{role.Key}\n";
            }
            output += $"```";
            return output;
        }

        private string HelpMessage(string prefix)
        {
            // Start Format
            string returnString = $"```\ntrigger commands: ADMIN ONLY:";
            // Add "add" command
            returnString += $"\n\tadd: Used to add a color role to the color changing role list";
            returnString += $"\n\t\tRequires a mentioned role";
            returnString += $"\n\t\tStructure: {prefix}color add <mentioned color role>";
            returnString += $"\n\t\tExample: {prefix}color add @red";
            // Add "remove" command
            returnString += $"\n\n\tremove: Used to remove a color role from the color changing role list";
            returnString += $"\n\t\tRequires a mentioned role";
            returnString += $"\n\t\tStructure: {prefix}color remove <mentioned color role>";
            returnString += $"\n\t\tExample: {prefix}color remove @red";
            //add "set" command
            returnString += $"\n\n\tset: Used to set the color changing role";
            returnString += $"\n\t\tRequires a mentioned role";
            returnString += $"\n\t\tStructure: {prefix}color set <desired chameleon role>";
            returnString += $"\n\t\tExample: {prefix}color set @chameleon";
            // add "unset" command
            returnString += $"\n\n\tunset: Used to remove the color changing role";
            returnString += $"\n\t\tRequires a mentioned role";
            returnString += $"\n\t\tStructure: {prefix}color unset";
            returnString += $"\n\t\tExample: {prefix}color unset";

            // END OF ADMIN COMMANDS
            returnString += $"\n\ntrigger commands: ALL USERS:";
            // Add "list" command
            returnString += $"\n\tlist: Displays a list of all current color roles";
            returnString += $"\n\t\tStructure: {prefix}color help";
            returnString += $"\n\t\tExample: {prefix}color help";
            // Add "help" command
            returnString += $"\n\thelp: Displays this help menu";
            returnString += $"\n\t\tStructure: {prefix}color help";
            returnString += $"\n\t\tExample: {prefix}color help";

            // Close formatting
            returnString += "```";
            return returnString;
        }
    }
}
