using CrewBot.Classes;
using System.Collections.Concurrent;

namespace CrewBot.Tests;

public class BotConfigurationSanitizerTests
{
    [Fact]
    public void Sanitize_RemovesMissingChannelsAndLeavesColorModuleEnabledForSetup()
    {
        BotConfig config = new BotConfig
        {
            LogChannelID = 11,
            DMChannelID = 22,
            GeneralChannelID = 33,
            ColorChannelID = 44,
            ColorRoleID = 55,
            ColorEnabled = true
        };

        var colorChoices = new ConcurrentDictionary<ulong, string>();
        colorChoices.TryAdd(66, "red");
        var voidChannels = new ConcurrentDictionary<ulong, VoidChannelConfiguration>();
        voidChannels.TryAdd(88, new VoidChannelConfiguration { ChannelID = 88, DefaultTimerSeconds = 60 });

        BotConfigurationSanitizerResult result = BotConfigurationSanitizer.Sanitize(
            config,
            availableChannelIds: new[] { 11ul },
            availableRoleIds: new[] { 77ul },
            colorChoices: colorChoices,
            voidChannels: voidChannels);

        Assert.Equal(11ul, config.LogChannelID);
        Assert.Equal(0ul, config.DMChannelID);
        Assert.Equal(0ul, config.GeneralChannelID);
        Assert.Equal(0ul, config.ColorChannelID);
        Assert.Equal(0ul, config.ColorRoleID);
        Assert.True(config.ColorEnabled);
        Assert.Empty(colorChoices);
        Assert.True(result.ConfigChanged);
        Assert.True(result.ColorChoicesChanged);
        Assert.True(result.VoidChannelsChanged);
        Assert.Contains(result.Warnings, warning => warning.Contains("DM relay channel"));
        Assert.Contains(result.Warnings, warning => warning.Contains("general channel"));
        Assert.Contains(result.Warnings, warning => warning.Contains("color channel"));
        Assert.Contains(result.Warnings, warning => warning.Contains("rotating color role"));
        Assert.Contains(result.Warnings, warning => warning.Contains("void channel"));
        Assert.DoesNotContain(result.Warnings, warning => warning.Contains("Color module was disabled"));
    }

    [Fact]
    public void Sanitize_WhenResourcesExist_DoesNotChangeConfig()
    {
        BotConfig config = new BotConfig
        {
            LogChannelID = 11,
            DMChannelID = 22,
            GeneralChannelID = 33,
            ColorChannelID = 44,
            ColorRoleID = 55,
            ColorEnabled = true
        };

        var colorChoices = new ConcurrentDictionary<ulong, string>();
        colorChoices.TryAdd(66, "red");
        var voidChannels = new ConcurrentDictionary<ulong, VoidChannelConfiguration>();
        voidChannels.TryAdd(44, new VoidChannelConfiguration { ChannelID = 44, DefaultTimerSeconds = 60 });

        BotConfigurationSanitizerResult result = BotConfigurationSanitizer.Sanitize(
            config,
            availableChannelIds: new[] { 11ul, 22ul, 33ul, 44ul },
            availableRoleIds: new[] { 55ul, 66ul },
            colorChoices: colorChoices,
            voidChannels: voidChannels);

        Assert.False(result.ConfigChanged);
        Assert.False(result.ColorChoicesChanged);
        Assert.False(result.VoidChannelsChanged);
        Assert.Empty(result.Warnings);
        Assert.True(config.ColorEnabled);
        Assert.Single(colorChoices);
        Assert.Single(voidChannels);
    }
}
