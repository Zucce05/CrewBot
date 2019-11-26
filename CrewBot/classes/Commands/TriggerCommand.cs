using CrewBot.Classes.Abstract;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace CrewBot.Classes.Commands
{
    public class TriggerCommand : MessageCommandBase
    {
        public string CommandString {get; private set;}

        public async Task AdminAction(SocketMessage message, ConcurrentDictionary<string, string> triggerResponses, BotConfig botConfig, string prefix, bool owner)
        {
            // example of newly refactored command using + as example token
            // +trigger add <xyz abc>
            // if not startswih token and COMMAND STRING log error and quit
            // What's next?  add? do add, remove? do remove, etc.
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
                                        botConfig.TriggerAdminEnabled = true;
                                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                                        await message.Channel.SendMessageAsync($"Administrator use for trigger commands is now enabled");
                                        break;
                                    case "disable":
                                        botConfig.TriggerAdminEnabled = false;
                                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                                        await message.Channel.SendMessageAsync($"Administrator use for trigger commands is now disabled");
                                        break;
                                    case "toggle":
                                        botConfig.TriggerAdminEnabled = !botConfig.TriggerAdminEnabled;
                                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                                        if (botConfig.TriggerAdminEnabled)
                                        {
                                            await message.Channel.SendMessageAsync($"Administrator use for trigger commands is now enabled");
                                        }
                                        else
                                        {
                                            await message.Channel.SendMessageAsync($"Administrator use for trigger commands is now disabled");
                                        }
                                        break;
                                }
                            }
                        break;
                    }
                }

                if (botConfig.TriggerEnabled || owner)
                {
                    switch (msg[1])
                    {
                        case "module":
                            if (msg.Length > 2)
                            {
                                switch (msg[2])
                                {
                                    case "enable":
                                        botConfig.TriggerEnabled = true;
                                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                                        await message.Channel.SendMessageAsync($"Module ``trigger`` is now enabled");
                                        break;
                                    case "disable":
                                        botConfig.TriggerEnabled = false;
                                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                                        await message.Channel.SendMessageAsync($"Module ``trigger`` is now disabled");
                                        break;
                                    case "toggle":
                                        botConfig.TriggerEnabled = !botConfig.TriggerEnabled;
                                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                                        if (botConfig.TriggerEnabled)
                                        {
                                            await message.Channel.SendMessageAsync($"Module ``trigger`` is now enabled");
                                        }
                                        else
                                        {
                                            await message.Channel.SendMessageAsync($"Module ``trigger`` is now disabled");
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }

                if (botConfig.TriggerEnabled)
                {
                    switch (msg[1])
                    {
                        case "add":
                            string[] substring = message.Content.Split("~");
                            if (substring.Length > 1)
                            {
                                if (triggerResponses.TryAdd(substring[1].Trim().ToLower(), substring[2].Trim()))
                                {
                                    SerializeJsonObject($"json/triggerResponses.json", triggerResponses);
                                    await message.Channel.SendMessageAsync($"triggerword {substring[1].Trim().ToLower()} response {substring[2].Trim().ToLower()} added.");
                                }
                                else
                                {
                                    await message.Channel.SendMessageAsync($"triggerword {substring[1].Trim().ToLower()} failed to be added.\n" +
                                        $"Use this format to add a triggerword ``+addtrigger -<keyword> -<desired response>``");
                                }
                            }
                            break;
                        case "remove":
                            string[] sub = message.Content.Split("~");
                            {
                                if (sub.Length > 1)
                                {
                                    if (triggerResponses.TryRemove(sub[1].Trim().ToLower(), out string value))
                                    {
                                        await message.Channel.SendMessageAsync($"triggerword: ``{sub[1].Trim().ToLower()}`` resulting in: ``{value}`` was removed");
                                        SerializeJsonObject($"json/triggerResponses.json", triggerResponses);
                                    }
                                    else
                                    {
                                        await message.Channel.SendMessageAsync($"{sub[1]} was **not** a triggerword or was not removed");
                                    }
                                }
                            }
                            break;
                        default:
                            await UserAction(message, triggerResponses, prefix);
                            break;
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync($"This module (``trigger``) is disabled");
                }
            }
        }

        public async Task UserAction(SocketMessage message, ConcurrentDictionary<string, string> triggerResponses, string prefix)
        {
            string[] msg = message.Content.ToLower().Split(new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries);
            switch (msg[1])
            {
                case "list":
                    await message.Channel.SendMessageAsync($"{ListCommand(triggerResponses)}");
                    break;
                case "help":
                    await message.Channel.SendMessageAsync($"{HelpMessage(prefix)}");
                    break;
            }
        }

        private string HelpMessage(string prefix)
        {
            // Start Format
            string returnString = $"```\ntrigger commands: ADMIN ONLY:";
            // Add "add" command
            returnString += $"\n\tadd: Used to add a custom trigger";
            returnString += $"\n\t\tRequires use of ~ (tilde) as delimiter";
            returnString += $"\n\t\tStructure: {prefix}trigger add ~<trigger message> ~<response action>";
            returnString += $"\n\t\tExample: {prefix}trigger add ~!test ~This is a test";
            // Add "remove" command
            returnString += $"\n\n\tremove: Used to remove a custom trigger";
            returnString += $"\n\t\tRequires use of ~ (tilde) as delimiter";
            returnString += $"\n\t\tStructure: {prefix}trigger remove ~<trigger message>";
            returnString += $"\n\t\tExample: {prefix}trigger remove ~!test";

            // END OF ADMIN COMMANDS
            returnString += $"\n\ntrigger commands: ALL USERS:";
            // Add "list" command
            returnString += $"\n\tlist: Displays a list of all available trigger action keys";
            returnString += $"\n\t\tStructure: {prefix}trigger list";
            returnString += $"\n\t\tExample: {prefix}trigger list";
            // Add "help" command
            returnString += $"\n\thelp: Displays this help menu";
            returnString += $"\n\t\tStructure: {prefix}trigger help";
            returnString += $"\n\t\tExample: {prefix}trigger help";

            // Close formatting
            returnString += "```";
            return returnString;
        }

        private string ListCommand(ConcurrentDictionary<string, string> triggerResponses)
        {
            string output = $"```\nCurrent Triggers:\n";
            foreach (string trigger in triggerResponses.Keys)
            {
                output += $"{trigger}\n";
            }
            output += $"```";
            return output;
        }

        //private void SerializeJsonObject(string filename, object value)
        //{
        //    _ = Program.Log(new LogMessage(LogSeverity.Verbose, $"Program", $"SerializeJson"));
        //    using (StreamWriter file = File.CreateText($"{filename}"))
        //    {
        //        JsonSerializer serializer = new JsonSerializer();
        //        serializer.Serialize(file, value);
        //    }
        //}
    }
}
