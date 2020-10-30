using Discord.WebSocket;
using CrewBot.Classes.Commands;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using Discord;

namespace CrewBot.Classes.FactoryClasses
{
    public static class MessageCommandFactory
    {
        public static async void MessageCommand(SocketMessage message,
            BotConfig botConfig,
            SocketGuild guild,
            ConcurrentDictionary<string, string> triggerResponses,
            ConcurrentDictionary<ulong, string> colorChoices)
        {
            bool serverOwner;
            try
            {
                serverOwner = message.Author.Id == guild.Owner.Id;
            }
            catch (System.NullReferenceException e)
            {
                _ = Program.Log(new LogMessage(LogSeverity.Error, $"MessageCommandFactory", $"{e.Message}"));
            }
            finally
            {
                serverOwner = false;
            }
            bool AdminAuthor = false;
            bool triggerAdded = false;
            try
            {
                foreach (SocketRole role in guild.GetUser(message.Author.Id).Roles)
                {
                    if (role.Id == botConfig.AdminID)
                    {
                        AdminAuthor = true;
                        break;
                    }
                }
            }
            catch(System.NullReferenceException e)
            {
                await Program.Log(new Discord.LogMessage(Discord.LogSeverity.Error, $"MessageCommand", $"{e.Message}"));
            }

            string[] msgBlocks = message.Content.ToLower().Split(" ");
            string msg = msgBlocks[0].Trim(new char[] { '+' });
            if (msgBlocks.Length > 1 && !message.Author.IsBot)
            {
                if (message.Content.StartsWith(botConfig.Prefix) && (AdminAuthor || serverOwner))
                {
                    switch (msg)
                    {
                        case "trigger":
                            if (serverOwner || (botConfig.TriggerAdminEnabled))
                            {
                                triggerAdded = true;
                                TriggerCommand trigger = new TriggerCommand();
                                await trigger.AdminAction(message, triggerResponses, botConfig, botConfig.Prefix, serverOwner);
                            }
                            else
                            {
                                await message.Channel.SendMessageAsync("This is currently disabled for admins.");
                            }
                            break;
                        case "color":
                            if (serverOwner || botConfig.ColorAdminEnabled)
                            {
                                ColorCommand color = new ColorCommand();
                                await color.AdminAction(message, colorChoices, botConfig.Prefix, botConfig, serverOwner);
                            }
                            else
                            {
                                await message.Channel.SendMessageAsync("This is currently disabled for admins.");
                            }
                            break;
                        case "settings":
                            if (serverOwner || botConfig.SettingsAdminEnabled)
                            {
                                SettingCommand setting = new SettingCommand();
                                await setting.AdminAction(message, botConfig, serverOwner);
                            }
                            else
                            {
                                await message.Channel.SendMessageAsync("This is currently disabled for admins.");
                            }
                            break;
                    }
                    // Get bot command from message string
                    // switch statement to create and return specific command object
                }
                else if (message.Content.StartsWith(botConfig.Prefix))
                {
                    switch (msg)
                    {
                        case "trigger":
                            if (botConfig.TriggerEnabled)
                            {
                                TriggerCommand trigger = new TriggerCommand();
                                await trigger.UserAction(message, triggerResponses, botConfig.Prefix);
                            }
                            else
                            {
                                await message.Channel.SendMessageAsync("This is currently disabled");
                            }
                            break;
                        case "color":
                            if (botConfig.ColorEnabled)
                            {
                                ColorCommand color = new ColorCommand();
                                await color.UserAction(message, colorChoices, botConfig.Prefix);
                            }
                            else
                            {
                                await message.Channel.SendMessageAsync("This is currently disabled");
                            }
                            break;
                        case "setting":
                            SettingCommand setting = new SettingCommand();
                            await setting.UserAction(message, botConfig);
                            break;
                    }
                }

            }
            if (!message.Author.IsBot)
            {
                if (message.Content.ToLower().StartsWith($"evetime"))
                {
                    await message.Channel.SendMessageAsync($"Current Eve game time: {DateTime.UtcNow.ToString()}");
                }

                if (botConfig.TriggerEnabled && !triggerAdded)
                {
                    foreach (KeyValuePair<string, string> kvp in triggerResponses)
                    {
                        if (message.Content.ToLower().Contains(kvp.Key))
                        {
                            await message.Channel.SendMessageAsync($"{kvp.Value}");
                            break;
                        }
                    }
                }
            }

        }
    }
}
