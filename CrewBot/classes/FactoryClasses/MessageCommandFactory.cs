using Discord;
using Discord.WebSocket;
using CrewBot.Classes.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CrewBot.Classes.FactoryClasses
{
    public static class MessageCommandFactory
    {
        public static async Task MessageCommand(
            SocketMessage message,
            BotConfig botConfig,
            SocketGuild guild,
            ConcurrentDictionary<string, string> triggerResponses,
            ConcurrentDictionary<ulong, string> colorChoices)
        {
            if (message == null || botConfig == null)
            {
                return;
            }

            bool serverOwner = false;
            try
            {
                if (message.Author != null && guild != null)
                {
                    serverOwner = IsServerOwner(message.Author.Id, guild.OwnerId, botConfig.AdditionalOwnerID);
                }
            }
            catch (NullReferenceException e)
            {
                _ = Program.Log(new LogMessage(LogSeverity.Error, "MessageCommandFactory", e.Message));
            }

            bool adminAuthor = false;
            try
            {
                SocketGuildUser guildUser = guild?.GetUser(message.Author.Id);
                if (guildUser != null)
                {
                    foreach (SocketRole role in guildUser.Roles)
                    {
                        if (role.Id == botConfig.AdminID)
                        {
                            adminAuthor = true;
                            break;
                        }
                    }
                }
            }
            catch (NullReferenceException e)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, "MessageCommandFactory", e.Message));
            }

            string content = message.Content ?? string.Empty;
            string prefix = MessageCommandParser.NormalizePrefix(botConfig.Prefix);
            bool isAuthorBot = message.Author?.IsBot ?? false;

            if (!isAuthorBot && SlashCommandFactory.IsMigratedPrefixCommand(content, prefix))
            {
                await message.Channel.SendMessageAsync(SlashCommandFactory.BuildPrefixMigrationMessage(content, prefix));
                return;
            }

            CommandRoutingDecision routingDecision = CommandRouting.Resolve(
                content,
                botConfig,
                adminAuthor,
                serverOwner,
                isAuthorBot);

            bool triggerAdded = false;

            switch (routingDecision.Route)
            {
                case CommandRoute.TriggerAdmin:
                    triggerAdded = true;
                    TriggerCommand triggerAdmin = new TriggerCommand();
                    await triggerAdmin.AdminAction(message, triggerResponses, botConfig, prefix, serverOwner);
                    break;

                case CommandRoute.ColorAdmin:
                    ColorCommand colorAdmin = new ColorCommand();
                    await colorAdmin.AdminAction(message, colorChoices, prefix, botConfig, serverOwner);
                    break;

                case CommandRoute.SettingsAdmin:
                    SettingCommand settingsAdmin = new SettingCommand();
                    await settingsAdmin.AdminAction(message, botConfig, serverOwner);
                    break;

                case CommandRoute.TriggerUser:
                    TriggerCommand triggerUser = new TriggerCommand();
                    await triggerUser.UserAction(message, triggerResponses, prefix);
                    break;

                case CommandRoute.ColorUser:
                    ColorCommand colorUser = new ColorCommand();
                    await colorUser.UserAction(message, colorChoices, prefix);
                    break;

                case CommandRoute.SettingsUser:
                    SettingCommand settingsUser = new SettingCommand();
                    await settingsUser.UserAction(message, botConfig);
                    break;

                case CommandRoute.AdminDisabled:
                case CommandRoute.ModuleDisabled:
                    if (!string.IsNullOrWhiteSpace(routingDecision.Message))
                    {
                        await message.Channel.SendMessageAsync(routingDecision.Message);
                    }

                    break;
            }

            if (!isAuthorBot)
            {
                if (content.ToLower().StartsWith("evetime"))
                {
                    await message.Channel.SendMessageAsync($"Current Eve game time: {DateTime.UtcNow}");
                }

                if (botConfig.TriggerEnabled && !triggerAdded)
                {
                    foreach (KeyValuePair<string, string> kvp in triggerResponses)
                    {
                        if (content.ToLower().Contains(kvp.Key))
                        {
                            await message.Channel.SendMessageAsync(kvp.Value);
                            break;
                        }
                    }
                }
            }
        }

        public static bool IsServerOwner(ulong authorId, ulong ownerId, ulong additionalOwnerId = 0)
        {
            return authorId == ownerId || (additionalOwnerId != 0 && authorId == additionalOwnerId);
        }
    }
}
