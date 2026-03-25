using CrewBot.Classes;
using CrewBot.Classes.Commands;

namespace CrewBot.Tests;

public class SettingCommandTests
{
    [Fact]
    public void BuildStatusMessage_IncludesKeyRuntimeValues()
    {
        BotConfig config = new BotConfig
        {
            BotEnabled = true,
            Prefix = "!",
            GuildID = 10,
            BotID = 20,
            AdminID = 30,
            LogChannelID = 40,
            DMChannelID = 50,
            GeneralChannelID = 60,
            ColorChannelID = 70,
            ColorRoleID = 80,
            ColorEnabled = true,
            ColorAdminEnabled = true,
            ColorTimerSeconds = 120,
            TriggerEnabled = true,
            TriggerAdminEnabled = false,
            SettingsAdminEnabled = true
        };

        string output = SettingCommand.BuildStatusMessage(config, isConnected: true, dataDirectoryPath: @"C:\CrewBotData");

        Assert.Contains("Connected: True", output);
        Assert.Contains("Prefix: !", output);
        Assert.Contains(@"DataDirectory: C:\CrewBotData", output);
        Assert.Contains("AdminRoleID: 30", output);
        Assert.Contains("ColorEnabled: True", output);
        Assert.Contains("TriggerAdminEnabled: False", output);
    }

    [Fact]
    public void BuildStatusMessage_WhenPrefixMissing_UsesPlusFallback()
    {
        string output = SettingCommand.BuildStatusMessage(new BotConfig(), isConnected: false, dataDirectoryPath: null);

        Assert.Contains("Connected: False", output);
        Assert.Contains("Prefix: +", output);
        Assert.Contains("DataDirectory: (unknown)", output);
    }
}
