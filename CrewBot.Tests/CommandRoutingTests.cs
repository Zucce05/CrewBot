using CrewBot.Classes;
using CrewBot.Classes.FactoryClasses;
using CrewBot.Classes.Commands;

namespace CrewBot.Tests;

public class CommandRoutingTests
{
    private static BotConfig MakeConfig() => new BotConfig
    {
        Prefix = "+",
        TriggerEnabled = true,
        ColorEnabled = true,
        SettingsAdminEnabled = true,
        TriggerAdminEnabled = true,
        ColorAdminEnabled = true
    };

    [Fact]
    public void Resolve_BotAuthor_ReturnsNone()
    {
        CommandRoutingDecision decision = CommandRouting.Resolve(
            "+trigger help",
            MakeConfig(),
            isAdmin: false,
            isOwner: false,
            isAuthorBot: true);

        Assert.Equal(CommandRoute.None, decision.Route);
    }

    [Fact]
    public void Resolve_AdminTriggerCommand_RoutesToTriggerAdmin()
    {
        CommandRoutingDecision decision = CommandRouting.Resolve(
            "+trigger help",
            MakeConfig(),
            isAdmin: true,
            isOwner: false,
            isAuthorBot: false);

        Assert.Equal(CommandRoute.TriggerAdmin, decision.Route);
    }

    [Fact]
    public void Resolve_AdminSettingsCommand_RoutesToSettingsAdmin()
    {
        CommandRoutingDecision decision = CommandRouting.Resolve(
            "+settings admin add @mods",
            MakeConfig(),
            isAdmin: true,
            isOwner: false,
            isAuthorBot: false);

        Assert.Equal(CommandRoute.SettingsAdmin, decision.Route);
    }

    [Fact]
    public void Resolve_UserTriggerCommand_RoutesToTriggerUser()
    {
        CommandRoutingDecision decision = CommandRouting.Resolve(
            "+trigger help",
            MakeConfig(),
            isAdmin: false,
            isOwner: false,
            isAuthorBot: false);

        Assert.Equal(CommandRoute.TriggerUser, decision.Route);
    }

    [Fact]
    public void Resolve_ColorDisabled_ReturnsModuleDisabledMessage()
    {
        BotConfig config = MakeConfig();
        config.ColorEnabled = false;

        CommandRoutingDecision decision = CommandRouting.Resolve(
            "+color list",
            config,
            isAdmin: false,
            isOwner: false,
            isAuthorBot: false);

        Assert.Equal(CommandRoute.ModuleDisabled, decision.Route);
        Assert.Equal("This is currently disabled", decision.Message);
    }

    [Fact]
    public void Resolve_AdminDisabled_ReturnsAdminDisabledMessage()
    {
        BotConfig config = MakeConfig();
        config.TriggerAdminEnabled = false;

        CommandRoutingDecision decision = CommandRouting.Resolve(
            "+trigger add ~a ~b",
            config,
            isAdmin: true,
            isOwner: false,
            isAuthorBot: false);

        Assert.Equal(CommandRoute.AdminDisabled, decision.Route);
        Assert.Equal("This is currently disabled for admins.", decision.Message);
    }

    [Theory]
    [InlineData("+settings help")]
    [InlineData("+setting help")]
    public void Resolve_SettingsAliases_RouteToSettingsUser(string message)
    {
        CommandRoutingDecision decision = CommandRouting.Resolve(
            message,
            MakeConfig(),
            isAdmin: false,
            isOwner: false,
            isAuthorBot: false);

        Assert.Equal(CommandRoute.SettingsUser, decision.Route);
    }

    [Theory]
    [InlineData("+setlogchannel")]
    [InlineData("+removelogchannel")]
    [InlineData("+setdmchannel")]
    [InlineData("+removedmchannel")]
    public void Resolve_LegacySettingsAliases_RouteToSettingsAdmin(string message)
    {
        CommandRoutingDecision decision = CommandRouting.Resolve(
            message,
            MakeConfig(),
            isAdmin: true,
            isOwner: false,
            isAuthorBot: false);

        Assert.Equal(CommandRoute.SettingsAdmin, decision.Route);
    }

    [Theory]
    [InlineData("+setlogchannel", "logchannel", "set")]
    [InlineData("+removelogchannel", "logchannel", "unset")]
    [InlineData("+setdmchannel", "dmchannel", "set")]
    [InlineData("+removedmchannel", "dmchannel", "unset")]
    public void NormalizeArguments_LegacySettingsAlias_MapsToSharedFlow(string message, string expectedCommand, string expectedAction)
    {
        string[] args = SettingCommand.NormalizeArguments(message);

        Assert.Equal(expectedCommand, args[1]);
        Assert.Equal(expectedAction, args[2]);
    }
}
