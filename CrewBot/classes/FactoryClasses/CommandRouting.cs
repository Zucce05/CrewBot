namespace CrewBot.Classes.FactoryClasses
{
    public enum CommandRoute
    {
        None,
        TriggerAdmin,
        ColorAdmin,
        SettingsAdmin,
        TriggerUser,
        ColorUser,
        SettingsUser,
        AdminDisabled,
        ModuleDisabled
    }

    public sealed class CommandRoutingDecision
    {
        public CommandRoute Route { get; set; } = CommandRoute.None;
        public string Message { get; set; } = string.Empty;
    }

    public static class CommandRouting
    {
        public static CommandRoutingDecision Resolve(
            string content,
            BotConfig botConfig,
            bool isAdmin,
            bool isOwner,
            bool isAuthorBot)
        {
            var decision = new CommandRoutingDecision();
            if (isAuthorBot)
            {
                return decision;
            }

            string prefix = MessageCommandParser.NormalizePrefix(botConfig?.Prefix);
            string command = MessageCommandParser.ExtractCommand(content, prefix);
            bool isPrefixed = MessageCommandParser.IsPrefixed(content, prefix);
            bool isLegacySettingsAdminAlias =
                command == "setlogchannel" ||
                command == "removelogchannel" ||
                command == "setdmchannel" ||
                command == "removedmchannel";
            string[] blocks = MessageCommandParser.GetBlocks(content);
            if (blocks.Length <= 1 && !isLegacySettingsAdminAlias)
            {
                return decision;
            }

            if (isPrefixed && (isAdmin || isOwner))
            {
                switch (command)
                {
                    case "trigger":
                        if (isOwner || botConfig.TriggerAdminEnabled)
                        {
                            decision.Route = CommandRoute.TriggerAdmin;
                        }
                        else
                        {
                            decision.Route = CommandRoute.AdminDisabled;
                            decision.Message = "This is currently disabled for admins.";
                        }

                        return decision;
                    case "color":
                        if (isOwner || botConfig.ColorAdminEnabled)
                        {
                            decision.Route = CommandRoute.ColorAdmin;
                        }
                        else
                        {
                            decision.Route = CommandRoute.AdminDisabled;
                            decision.Message = "This is currently disabled for admins.";
                        }

                        return decision;
                    case "settings":
                    case "setlogchannel":
                    case "removelogchannel":
                    case "setdmchannel":
                    case "removedmchannel":
                        if (isOwner || botConfig.SettingsAdminEnabled)
                        {
                            decision.Route = CommandRoute.SettingsAdmin;
                        }
                        else
                        {
                            decision.Route = CommandRoute.AdminDisabled;
                            decision.Message = "This is currently disabled for admins.";
                        }

                        return decision;
                }

                return decision;
            }

            if (isPrefixed && !isLegacySettingsAdminAlias)
            {
                switch (command)
                {
                    case "trigger":
                        if (botConfig.TriggerEnabled)
                        {
                            decision.Route = CommandRoute.TriggerUser;
                        }
                        else
                        {
                            decision.Route = CommandRoute.ModuleDisabled;
                            decision.Message = "This is currently disabled";
                        }

                        return decision;
                    case "color":
                        if (botConfig.ColorEnabled)
                        {
                            decision.Route = CommandRoute.ColorUser;
                        }
                        else
                        {
                            decision.Route = CommandRoute.ModuleDisabled;
                            decision.Message = "This is currently disabled";
                        }

                        return decision;
                    case "setting":
                    case "settings":
                        decision.Route = CommandRoute.SettingsUser;
                        return decision;
                }
            }

            return decision;
        }
    }
}
