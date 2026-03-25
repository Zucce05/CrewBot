using CrewBot.Classes.Commands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CrewBot.Classes.FactoryClasses
{
    public static class SlashCommandFactory
    {
        private static readonly IJsonFileStore JsonStore = new JsonFileStore();

        public static IReadOnlyCollection<ApplicationCommandProperties> BuildGuildCommands()
        {
            return new ApplicationCommandProperties[]
            {
                BuildColorCommand().Build(),
                BuildTriggerCommand().Build(),
                BuildSettingsCommand().Build(),
                BuildVoidCommand().Build()
            };
        }

        public static bool IsMigratedPrefixCommand(string content, string prefix)
        {
            if (!MessageCommandParser.IsPrefixed(content, prefix))
            {
                return false;
            }

            switch (MessageCommandParser.ExtractCommand(content, prefix))
            {
                case "color":
                case "trigger":
                case "setting":
                case "settings":
                case "setlogchannel":
                case "removelogchannel":
                case "setdmchannel":
                case "removedmchannel":
                    return true;
                default:
                    return false;
            }
        }

        public static string BuildPrefixMigrationMessage(string content, string prefix)
        {
            string command = MessageCommandParser.ExtractCommand(content, prefix);
            switch (command)
            {
                case "color":
                    return "Color configuration now uses slash commands. Try `/color list`, `/color role add`, or `/color module enable`.";
                case "trigger":
                    return "Trigger configuration now uses slash commands. Try `/trigger list`, `/trigger add`, or `/trigger module enable`. Passive trigger responses still activate from normal messages.";
                case "setting":
                case "settings":
                case "setlogchannel":
                case "removelogchannel":
                case "setdmchannel":
                case "removedmchannel":
                    return "Settings configuration now uses slash commands. Try `/settings status`, `/settings channel set-log`, or `/settings admin-role set`.";
                default:
                    return "This configuration command has moved to slash commands.";
            }
        }

        public static async Task RegisterGuildCommandsAsync(SocketGuild guild)
        {
            if (guild == null)
            {
                return;
            }

            await guild.BulkOverwriteApplicationCommandAsync(BuildGuildCommands().ToArray());
        }

        public static async Task HandleAsync(
            SocketSlashCommand command,
            BotConfig botConfig,
            SocketGuild guild,
            ConcurrentDictionary<string, string> triggerResponses,
            ConcurrentDictionary<ulong, string> colorChoices,
            ConcurrentDictionary<ulong, VoidChannelConfiguration> voidChannels)
        {
            if (command == null || botConfig == null || guild == null)
            {
                return;
            }

            SocketGuildUser guildUser = command.User as SocketGuildUser ?? guild.GetUser(command.User.Id);
            bool isOwner = MessageCommandFactory.IsServerOwner(command.User.Id, guild.OwnerId, botConfig.AdditionalOwnerID);
            bool isAdmin = guildUser?.Roles.Any(role => role.Id == botConfig.AdminID) ?? false;
            SlashCommandRequest request = SlashCommandRequest.Create(command);

            switch (command.Data.Name)
            {
                case "color":
                    await HandleColorAsync(command, request, botConfig, colorChoices, isOwner, isAdmin);
                    return;
                case "trigger":
                    await HandleTriggerAsync(command, request, botConfig, triggerResponses, isOwner, isAdmin);
                    return;
                case "settings":
                    await HandleSettingsAsync(command, request, botConfig, isOwner, isAdmin);
                    return;
                case "void":
                    await HandleVoidAsync(command, request, botConfig, voidChannels, isOwner, isAdmin);
                    return;
            }
        }

        private static async Task HandleColorAsync(
            SocketSlashCommand command,
            SlashCommandRequest request,
            BotConfig botConfig,
            ConcurrentDictionary<ulong, string> colorChoices,
            bool isOwner,
            bool isAdmin)
        {
            if (request.Subcommand == "list")
            {
                string output = "```\nCurrent Color Roles:\n";
                foreach (KeyValuePair<ulong, string> role in colorChoices)
                {
                    output += $"Name: {role.Value} \tID:{role.Key}\n";
                }

                output += "```";
                await command.RespondAsync(output);
                return;
            }

            if (request.Subcommand == "help")
            {
                await command.RespondAsync("Use `/color list` to view roles, `/color role add` or `/color role remove` to manage choices, `/color rotating set` to set the chameleon role, and `/color module enable` to turn the feature on.");
                return;
            }

            bool canManageColor = isOwner || (isAdmin && botConfig.ColorAdminEnabled);
            if (request.Group == "admin")
            {
                if (!isOwner)
                {
                    await command.RespondAsync("Only the server owner can manage color admin access.", ephemeral: true);
                    return;
                }

                await command.RespondAsync(SetToggleState(
                    request.Subcommand,
                    () => botConfig.ColorAdminEnabled,
                    value => botConfig.ColorAdminEnabled = value,
                    "Administrator use for ``color`` commands is now enabled",
                    "Administrator use for ``color`` commands is now disabled",
                    botConfig));
                return;
            }

            if (!canManageColor)
            {
                await command.RespondAsync("You do not have permission to manage color configuration.", ephemeral: true);
                return;
            }

            switch (request.Group)
            {
                case "module":
                    await command.RespondAsync(SetToggleState(
                        request.Subcommand,
                        () => botConfig.ColorEnabled,
                        value => botConfig.ColorEnabled = value,
                        "Module ``color`` is now enabled",
                        "Module ``color`` is now disabled",
                        botConfig));
                    return;
                case "role":
                    SocketRole managedRole = request.GetRole("role");
                    if (managedRole == null)
                    {
                        await command.RespondAsync("You must specify a role.", ephemeral: true);
                        return;
                    }

                    if (request.Subcommand == "add")
                    {
                        if (colorChoices.TryAdd(managedRole.Id, managedRole.Name))
                        {
                            JsonStore.Save(BotStartup.ColorChoicesPath, colorChoices);
                            await command.RespondAsync($"Color ``{managedRole.Name}`` added");
                        }
                        else
                        {
                            await command.RespondAsync($"Color ``{managedRole.Name}`` was already configured");
                        }

                        return;
                    }

                    if (request.Subcommand == "remove")
                    {
                        if (colorChoices.TryRemove(managedRole.Id, out _))
                        {
                            JsonStore.Save(BotStartup.ColorChoicesPath, colorChoices);
                            await command.RespondAsync($"Color ``{managedRole.Name}`` removed");
                        }
                        else
                        {
                            await command.RespondAsync($"Color ``{managedRole.Name}`` was not configured");
                        }

                        return;
                    }

                    break;
                case "rotating":
                    if (request.Subcommand == "set")
                    {
                        SocketRole rotatingRole = request.GetRole("role");
                        if (rotatingRole == null)
                        {
                            await command.RespondAsync("You must specify a role.", ephemeral: true);
                            return;
                        }

                        botConfig.ColorRoleID = rotatingRole.Id;
                        BotStartup.SaveConfig(botConfig);
                        await command.RespondAsync($"Rotating color role set to ``{rotatingRole.Name}``");
                        return;
                    }

                    if (request.Subcommand == "unset")
                    {
                        botConfig.ColorRoleID = 0;
                        BotStartup.SaveConfig(botConfig);
                        await command.RespondAsync("Rotating color role has been unset");
                        return;
                    }

                    break;
            }

            await command.RespondAsync("Unsupported color command.", ephemeral: true);
        }

        private static async Task HandleTriggerAsync(
            SocketSlashCommand command,
            SlashCommandRequest request,
            BotConfig botConfig,
            ConcurrentDictionary<string, string> triggerResponses,
            bool isOwner,
            bool isAdmin)
        {
            if (request.Subcommand == "list")
            {
                string output = "```\nCurrent Triggers:\n";
                foreach (string trigger in triggerResponses.Keys)
                {
                    output += $"{trigger}\n";
                }

                output += "```";
                await command.RespondAsync(output);
                return;
            }

            if (request.Subcommand == "help")
            {
                await command.RespondAsync("Use `/trigger add` or `/trigger remove` to manage trigger phrases, and `/trigger module enable` to turn automatic trigger replies on. Trigger phrases still activate from normal messages.");
                return;
            }

            bool canManageTrigger = isOwner || (isAdmin && botConfig.TriggerAdminEnabled);
            if (request.Group == "admin")
            {
                if (!isOwner)
                {
                    await command.RespondAsync("Only the server owner can manage trigger admin access.", ephemeral: true);
                    return;
                }

                await command.RespondAsync(SetToggleState(
                    request.Subcommand,
                    () => botConfig.TriggerAdminEnabled,
                    value => botConfig.TriggerAdminEnabled = value,
                    "Administrator use for trigger commands is now enabled",
                    "Administrator use for trigger commands is now disabled",
                    botConfig));
                return;
            }

            if (!canManageTrigger)
            {
                await command.RespondAsync("You do not have permission to manage trigger configuration.", ephemeral: true);
                return;
            }

            if (request.Group == "module")
            {
                await command.RespondAsync(SetToggleState(
                    request.Subcommand,
                    () => botConfig.TriggerEnabled,
                    value => botConfig.TriggerEnabled = value,
                    "Module ``trigger`` is now enabled",
                    "Module ``trigger`` is now disabled",
                    botConfig));
                return;
            }

            if (request.Subcommand == "add")
            {
                string keyword = request.GetString("keyword")?.Trim().ToLowerInvariant();
                string response = request.GetString("response")?.Trim();
                if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(response))
                {
                    await command.RespondAsync("You must provide both a keyword and a response.", ephemeral: true);
                    return;
                }

                if (triggerResponses.TryAdd(keyword, response))
                {
                    JsonStore.Save(BotStartup.TriggerResponsesPath, triggerResponses);
                    await command.RespondAsync($"triggerword {keyword} response {response.ToLowerInvariant()} added.");
                }
                else
                {
                    await command.RespondAsync($"triggerword {keyword} failed to be added.");
                }

                return;
            }

            if (request.Subcommand == "remove")
            {
                string keyword = request.GetString("keyword")?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    await command.RespondAsync("You must provide a keyword.", ephemeral: true);
                    return;
                }

                if (triggerResponses.TryRemove(keyword, out string value))
                {
                    JsonStore.Save(BotStartup.TriggerResponsesPath, triggerResponses);
                    await command.RespondAsync($"triggerword: ``{keyword}`` resulting in: ``{value}`` was removed");
                }
                else
                {
                    await command.RespondAsync($"{keyword} was **not** a triggerword or was not removed");
                }

                return;
            }

            await command.RespondAsync("Unsupported trigger command.", ephemeral: true);
        }

        private static async Task HandleSettingsAsync(
            SocketSlashCommand command,
            SlashCommandRequest request,
            BotConfig botConfig,
            bool isOwner,
            bool isAdmin)
        {
            if (request.Subcommand == "status")
            {
                await command.RespondAsync(SettingCommand.BuildStatusMessage(
                    botConfig,
                    Program.client?.ConnectionState == ConnectionState.Connected,
                    BotStartup.DataDirectoryPath));
                return;
            }

            if (request.Subcommand == "help")
            {
                await command.RespondAsync("Use `/settings status` to inspect runtime config, `/settings channel ...` to set channels, `/settings admin-role set` to configure the custom admin role, and `/settings bot enable` or `/settings bot disable` for owner-only bot control.");
                return;
            }

            bool canManageSettings = isOwner || (isAdmin && botConfig.SettingsAdminEnabled);
            switch (request.Group)
            {
                case "admin-role":
                    if (!isOwner)
                    {
                        await command.RespondAsync("Only the server owner can add or remove the bot admin role.", ephemeral: true);
                        return;
                    }

                    if (request.Subcommand == "set")
                    {
                        SocketRole adminRole = request.GetRole("role");
                        if (adminRole == null)
                        {
                            await command.RespondAsync("You must specify a role.", ephemeral: true);
                            return;
                        }

                        botConfig.AdminID = adminRole.Id;
                        BotStartup.SaveConfig(botConfig);
                        await command.RespondAsync($"Role: {adminRole.Name} with ID:{adminRole.Id} was set as admin;");
                        return;
                    }

                    if (request.Subcommand == "clear")
                    {
                        botConfig.AdminID = 0;
                        BotStartup.SaveConfig(botConfig);
                        await command.RespondAsync("ADMIN role has been unset");
                        return;
                    }

                    break;
                case "bot":
                    if (!isOwner)
                    {
                        await command.RespondAsync("Only the server owner can enable or disable the bot runtime.", ephemeral: true);
                        return;
                    }

                    if (request.Subcommand == "enable")
                    {
                        botConfig.BotEnabled = true;
                        BotStartup.SaveConfig(botConfig);
                        await command.RespondAsync("Bot runtime is now enabled in config");
                        return;
                    }

                    if (request.Subcommand == "disable")
                    {
                        botConfig.BotEnabled = false;
                        BotStartup.SaveConfig(botConfig);
                        await command.RespondAsync("Bot runtime is now disabled in config");
                        return;
                    }

                    break;
                case "channel":
                    if (!canManageSettings)
                    {
                        await command.RespondAsync("You do not have permission to manage bot settings.", ephemeral: true);
                        return;
                    }

                    await HandleSettingsChannelAsync(command, request, botConfig);
                    return;
                case "message-logging":
                    if (!canManageSettings)
                    {
                        await command.RespondAsync("You do not have permission to manage bot settings.", ephemeral: true);
                        return;
                    }

                    await HandleMessageLoggingAsync(command, request);
                    return;
            }

            await command.RespondAsync("Unsupported settings command.", ephemeral: true);
        }

        private static async Task HandleVoidAsync(
            SocketSlashCommand command,
            SlashCommandRequest request,
            BotConfig botConfig,
            ConcurrentDictionary<ulong, VoidChannelConfiguration> voidChannels,
            bool isOwner,
            bool isAdmin)
        {
            bool canManageVoid = isOwner || (isAdmin && botConfig.SettingsAdminEnabled);
            if (!canManageVoid)
            {
                await command.RespondAsync("You do not have permission to manage void channels.", ephemeral: true);
                return;
            }

            if (request.Subcommand == "list")
            {
                if (voidChannels.Count == 0)
                {
                    await command.RespondAsync("No void channels are configured.");
                    return;
                }

                string output = "```\nConfigured Void Channels:\n";
                foreach (VoidChannelConfiguration voidChannel in voidChannels.Values.OrderBy(channel => channel.ChannelID))
                {
                    output += $"ChannelID: {voidChannel.ChannelID} \tDefaultTimerSeconds:{voidChannel.DefaultTimerSeconds}\n";
                }

                output += "```";
                await command.RespondAsync(output);
                return;
            }

            SocketGuildChannel channel = request.GetChannel("channel");
            if (channel == null)
            {
                await command.RespondAsync("You must specify a channel.", ephemeral: true);
                return;
            }

            if (channel is not SocketTextChannel)
            {
                await command.RespondAsync("Void channels must be standard text channels.", ephemeral: true);
                return;
            }

            if (request.Subcommand == "unset")
            {
                if (voidChannels.TryRemove(channel.Id, out _))
                {
                    JsonStore.Save(BotStartup.VoidChannelsPath, voidChannels);
                    await command.RespondAsync($"Channel <#{channel.Id}> is no longer a void channel.");
                }
                else
                {
                    await command.RespondAsync($"Channel <#{channel.Id}> was not configured as a void channel.");
                }

                return;
            }

            string durationText = request.GetString("duration");
            if (!VoidTimingParser.TryParseDuration(durationText, out int durationSeconds))
            {
                await command.RespondAsync("Duration must use the format `10s`, `10m`, `2h`, or `1d`.", ephemeral: true);
                return;
            }

            var configuration = new VoidChannelConfiguration
            {
                ChannelID = channel.Id,
                DefaultTimerSeconds = durationSeconds
            };

            voidChannels.AddOrUpdate(channel.Id, configuration, (_, existing) =>
            {
                existing.DefaultTimerSeconds = durationSeconds;
                return existing;
            });

            JsonStore.Save(BotStartup.VoidChannelsPath, voidChannels);

            string action = request.Subcommand == "set" ? "is now" : "default timer updated";
            await command.RespondAsync($"Channel <#{channel.Id}> {action} a void channel with a default timer of {durationSeconds} second(s).");
        }

        private static async Task HandleSettingsChannelAsync(
            SocketSlashCommand command,
            SlashCommandRequest request,
            BotConfig botConfig)
        {
            SocketGuildChannel channel = request.GetChannel("channel");
            switch (request.Subcommand)
            {
                case "set-log":
                    if (channel == null)
                    {
                        await command.RespondAsync("You must specify a channel.", ephemeral: true);
                        return;
                    }

                    botConfig.LogChannelID = channel.Id;
                    BotStartup.SaveConfig(botConfig);
                    await command.RespondAsync($"Log Channel is now #{channel.Name}, ID:{channel.Id}");
                    return;
                case "clear-log":
                    botConfig.LogChannelID = 0;
                    BotStartup.SaveConfig(botConfig);
                    await command.RespondAsync("Log Channel has been unset");
                    return;
                case "set-dm":
                    if (channel == null)
                    {
                        await command.RespondAsync("You must specify a channel.", ephemeral: true);
                        return;
                    }

                    botConfig.DMChannelID = channel.Id;
                    BotStartup.SaveConfig(botConfig);
                    await command.RespondAsync($"DM Channel is now #{channel.Name}, ID:{channel.Id}");
                    return;
                case "clear-dm":
                    botConfig.DMChannelID = 0;
                    BotStartup.SaveConfig(botConfig);
                    await command.RespondAsync("DM Channel has been unset");
                    return;
                case "set-color":
                    if (channel == null)
                    {
                        await command.RespondAsync("You must specify a channel.", ephemeral: true);
                        return;
                    }

                    botConfig.ColorChannelID = channel.Id;
                    BotStartup.SaveConfig(botConfig);
                    await command.RespondAsync($"Color Channel is now #{channel.Name}, ID:{channel.Id}");
                    return;
                case "clear-color":
                    botConfig.ColorChannelID = 0;
                    BotStartup.SaveConfig(botConfig);
                    await command.RespondAsync("Color Channel has been unset");
                    return;
                case "set-general":
                    if (channel == null)
                    {
                        await command.RespondAsync("You must specify a channel.", ephemeral: true);
                        return;
                    }

                    botConfig.GeneralChannelID = channel.Id;
                    BotStartup.SaveConfig(botConfig);
                    await command.RespondAsync($"General Channel is now #{channel.Name}, ID:{channel.Id}");
                    return;
                case "clear-general":
                    botConfig.GeneralChannelID = 0;
                    BotStartup.SaveConfig(botConfig);
                    await command.RespondAsync("General Channel has been unset");
                    return;
            }

            await command.RespondAsync("Unsupported channel settings command.", ephemeral: true);
        }

        private static async Task HandleMessageLoggingAsync(SocketSlashCommand command, SlashCommandRequest request)
        {
            SocketGuildChannel channel = request.GetChannel("channel");
            if (channel == null)
            {
                await command.RespondAsync("You must specify a channel.", ephemeral: true);
                return;
            }

            switch (request.Subcommand)
            {
                case "disable":
                    if (!Program.ignoreMessagesCache.Contains(channel.Id))
                    {
                        Program.ignoreMessagesCache.Add(channel.Id);
                        JsonStore.Save(BotStartup.IgnoreMessageCachePath, Program.ignoreMessagesCache);
                    }

                    await command.RespondAsync($"Message logging in <#{channel.Id}> ID:{channel.Id} is disabled");
                    return;
                case "enable":
                    if (Program.ignoreMessagesCache.Contains(channel.Id))
                    {
                        Program.ignoreMessagesCache.Remove(channel.Id);
                        JsonStore.Save(BotStartup.IgnoreMessageCachePath, Program.ignoreMessagesCache);
                    }

                    await command.RespondAsync($"Message logging in #<{channel.Id}> ID:{channel.Id} is enabled");
                    return;
            }

            await command.RespondAsync("Unsupported message logging command.", ephemeral: true);
        }

        private static string SetToggleState(
            string action,
            Func<bool> getter,
            Action<bool> setter,
            string enabledMessage,
            string disabledMessage,
            BotConfig botConfig)
        {
            switch (action)
            {
                case "enable":
                    setter(true);
                    BotStartup.SaveConfig(botConfig);
                    return enabledMessage;
                case "disable":
                    setter(false);
                    BotStartup.SaveConfig(botConfig);
                    return disabledMessage;
                case "toggle":
                    setter(!getter());
                    BotStartup.SaveConfig(botConfig);
                    return getter() ? enabledMessage : disabledMessage;
                default:
                    return "Unsupported toggle action.";
            }
        }

        private static SlashCommandBuilder BuildColorCommand()
        {
            var builder = new SlashCommandBuilder()
                .WithName("color")
                .WithDescription("Manage color roles and rotating color settings.");

            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("list")
                .WithDescription("List configured color roles.")
                .WithType(ApplicationCommandOptionType.SubCommand));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("help")
                .WithDescription("Show color command help.")
                .WithType(ApplicationCommandOptionType.SubCommand));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("role")
                .WithDescription("Manage available color roles.")
                .WithType(ApplicationCommandOptionType.SubCommandGroup)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("add")
                    .WithDescription("Add a color role choice.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("role", ApplicationCommandOptionType.Role, "The role to add.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("remove")
                    .WithDescription("Remove a color role choice.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("role", ApplicationCommandOptionType.Role, "The role to remove.", isRequired: true)));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("rotating")
                .WithDescription("Manage the rotating color role.")
                .WithType(ApplicationCommandOptionType.SubCommandGroup)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("set")
                    .WithDescription("Set the rotating color role.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("role", ApplicationCommandOptionType.Role, "The rotating role.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("unset")
                    .WithDescription("Unset the rotating color role.")
                    .WithType(ApplicationCommandOptionType.SubCommand)));
            builder.AddOption(BuildToggleGroup("module", "Manage the color module."));
            builder.AddOption(BuildToggleGroup("admin", "Manage color admin access."));

            return builder;
        }

        private static SlashCommandBuilder BuildTriggerCommand()
        {
            var builder = new SlashCommandBuilder()
                .WithName("trigger")
                .WithDescription("Manage trigger responses.");

            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("list")
                .WithDescription("List configured trigger phrases.")
                .WithType(ApplicationCommandOptionType.SubCommand));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("help")
                .WithDescription("Show trigger command help.")
                .WithType(ApplicationCommandOptionType.SubCommand));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("add")
                .WithDescription("Add a trigger phrase.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("keyword", ApplicationCommandOptionType.String, "The trigger phrase to match.", isRequired: true)
                .AddOption("response", ApplicationCommandOptionType.String, "The response to send.", isRequired: true));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("remove")
                .WithDescription("Remove a trigger phrase.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("keyword", ApplicationCommandOptionType.String, "The trigger phrase to remove.", isRequired: true));
            builder.AddOption(BuildToggleGroup("module", "Manage the trigger module."));
            builder.AddOption(BuildToggleGroup("admin", "Manage trigger admin access."));

            return builder;
        }

        private static SlashCommandBuilder BuildSettingsCommand()
        {
            var builder = new SlashCommandBuilder()
                .WithName("settings")
                .WithDescription("Manage CrewBot settings.");

            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("status")
                .WithDescription("Show runtime and configuration status.")
                .WithType(ApplicationCommandOptionType.SubCommand));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("help")
                .WithDescription("Show settings command help.")
                .WithType(ApplicationCommandOptionType.SubCommand));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("admin-role")
                .WithDescription("Manage the custom admin role.")
                .WithType(ApplicationCommandOptionType.SubCommandGroup)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("set")
                    .WithDescription("Set the custom admin role.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("role", ApplicationCommandOptionType.Role, "The admin role.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("clear")
                    .WithDescription("Clear the custom admin role.")
                    .WithType(ApplicationCommandOptionType.SubCommand)));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("bot")
                .WithDescription("Manage owner-only bot runtime settings.")
                .WithType(ApplicationCommandOptionType.SubCommandGroup)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("enable")
                    .WithDescription("Enable the bot runtime.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("disable")
                    .WithDescription("Disable the bot runtime.")
                    .WithType(ApplicationCommandOptionType.SubCommand)));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("channel")
                .WithDescription("Manage configured bot channels.")
                .WithType(ApplicationCommandOptionType.SubCommandGroup)
                .AddOption(BuildChannelSubcommand("set-log", "Set the log channel.", true))
                .AddOption(BuildChannelSubcommand("clear-log", "Clear the log channel.", false))
                .AddOption(BuildChannelSubcommand("set-dm", "Set the DM relay channel.", true))
                .AddOption(BuildChannelSubcommand("clear-dm", "Clear the DM relay channel.", false))
                .AddOption(BuildChannelSubcommand("set-color", "Set the color info channel.", true))
                .AddOption(BuildChannelSubcommand("clear-color", "Clear the color info channel.", false))
                .AddOption(BuildChannelSubcommand("set-general", "Set the general channel.", true))
                .AddOption(BuildChannelSubcommand("clear-general", "Clear the general channel.", false)));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("message-logging")
                .WithDescription("Enable or disable message logging for a channel.")
                .WithType(ApplicationCommandOptionType.SubCommandGroup)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("enable")
                    .WithDescription("Enable message logging in a channel.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel to enable.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("disable")
                    .WithDescription("Disable message logging in a channel.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel to disable.", isRequired: true)));

            return builder;
        }

        private static SlashCommandBuilder BuildVoidCommand()
        {
            var builder = new SlashCommandBuilder()
                .WithName("void")
                .WithDescription("Manage temporary-message void channels.");

            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("list")
                .WithDescription("List configured void channels.")
                .WithType(ApplicationCommandOptionType.SubCommand));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("set")
                .WithDescription("Create or replace a void channel configuration.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("channel", ApplicationCommandOptionType.Channel, "The void channel.", isRequired: true)
                .AddOption("duration", ApplicationCommandOptionType.String, "Default delete timer like 10m or 1h.", isRequired: true));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("default")
                .WithDescription("Update the default delete timer for a void channel.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("channel", ApplicationCommandOptionType.Channel, "The void channel.", isRequired: true)
                .AddOption("duration", ApplicationCommandOptionType.String, "Default delete timer like 10m or 1h.", isRequired: true));
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("unset")
                .WithDescription("Remove a void channel configuration.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("channel", ApplicationCommandOptionType.Channel, "The void channel.", isRequired: true));

            return builder;
        }

        private static SlashCommandOptionBuilder BuildToggleGroup(string name, string description)
        {
            return new SlashCommandOptionBuilder()
                .WithName(name)
                .WithDescription(description)
                .WithType(ApplicationCommandOptionType.SubCommandGroup)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("enable")
                    .WithDescription($"Enable {name}.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("disable")
                    .WithDescription($"Disable {name}.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("toggle")
                    .WithDescription($"Toggle {name}.")
                    .WithType(ApplicationCommandOptionType.SubCommand));
        }

        private static SlashCommandOptionBuilder BuildChannelSubcommand(string name, string description, bool requiresChannel)
        {
            var builder = new SlashCommandOptionBuilder()
                .WithName(name)
                .WithDescription(description)
                .WithType(ApplicationCommandOptionType.SubCommand);

            if (requiresChannel)
            {
                builder.AddOption("channel", ApplicationCommandOptionType.Channel, "The channel to configure.", isRequired: true);
            }

            return builder;
        }

        private sealed class SlashCommandRequest
        {
            public string Group { get; private set; } = string.Empty;
            public string Subcommand { get; private set; } = string.Empty;
            public IReadOnlyCollection<SocketSlashCommandDataOption> Options { get; private set; } = Array.Empty<SocketSlashCommandDataOption>();

            public static SlashCommandRequest Create(SocketSlashCommand command)
            {
                var request = new SlashCommandRequest();
                SocketSlashCommandDataOption topLevel = command?.Data?.Options?.FirstOrDefault();
                if (topLevel == null)
                {
                    return request;
                }

                if (topLevel.Type == ApplicationCommandOptionType.SubCommandGroup)
                {
                    request.Group = topLevel.Name;
                    SocketSlashCommandDataOption subcommand = topLevel.Options?.FirstOrDefault();
                    request.Subcommand = subcommand?.Name ?? string.Empty;
                    request.Options = subcommand?.Options?.ToArray() ?? Array.Empty<SocketSlashCommandDataOption>();
                    return request;
                }

                request.Subcommand = topLevel.Name;
                request.Options = topLevel.Options?.ToArray() ?? Array.Empty<SocketSlashCommandDataOption>();
                return request;
            }

            public string GetString(string name) => GetOption(name)?.Value?.ToString();
            public SocketRole GetRole(string name) => GetOption(name)?.Value as SocketRole;
            public SocketGuildChannel GetChannel(string name) => GetOption(name)?.Value as SocketGuildChannel;

            private SocketSlashCommandDataOption GetOption(string name)
            {
                return Options.FirstOrDefault(option => string.Equals(option.Name, name, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
