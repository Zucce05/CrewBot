using CrewBot.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace CrewBot.Tests;

public class BotStartupTests : IDisposable
{
    private readonly string _originalDirectory;
    private readonly string? _originalTokenEnvironmentValue;
    private readonly string _testRoot;
    private readonly string _dataRoot;

    public BotStartupTests()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _originalTokenEnvironmentValue = Environment.GetEnvironmentVariable(BotStartup.TokenEnvironmentVariableName);
        _testRoot = Path.Combine(Path.GetTempPath(), "CrewBotStartupTests", Guid.NewGuid().ToString("N"));
        _dataRoot = Path.Combine(_testRoot, "CrewBot", "json");
        Directory.CreateDirectory(_testRoot);
        Directory.SetCurrentDirectory(_testRoot);
        BotStartup.SetDataDirectoryOverride(_dataRoot);
        Environment.SetEnvironmentVariable(BotStartup.TokenEnvironmentVariableName, null);
    }

    [Fact]
    public void Startup_WhenFilesMissing_CreatesExpectedJsonFiles()
    {
        BotConfig config = new BotConfig();
        var colors = new ConcurrentDictionary<ulong, string>();
        var triggers = new ConcurrentDictionary<string, string>();
        var messages = new ConcurrentDictionary<ulong, LoggedMessage>();
        var ignore = new List<ulong>();
        var voidChannels = new ConcurrentDictionary<ulong, VoidChannelConfiguration>();
        var voidMessages = new ConcurrentDictionary<ulong, ScheduledVoidMessage>();

        BotStartup.Startup(ref config, ref colors, ref triggers, ref messages, ref ignore, ref voidChannels, ref voidMessages);

        Assert.True(File.Exists(BotStartup.BotConfigPath));
        Assert.True(File.Exists(BotStartup.ColorChoicesPath));
        Assert.True(File.Exists(BotStartup.TriggerResponsesPath));
        Assert.True(File.Exists(BotStartup.MessageCachePath));
        Assert.True(File.Exists(BotStartup.IgnoreMessageCachePath));
        Assert.True(File.Exists(BotStartup.VoidChannelsPath));
        Assert.True(File.Exists(BotStartup.VoidMessagesPath));
    }

    [Fact]
    public void Startup_WhenTemplateExists_LoadsTemplateConfig()
    {
        Directory.CreateDirectory(_dataRoot);
        File.WriteAllText(Path.Combine(_dataRoot, "BotConfig - Copy.json"),
@"// comment line
{
  ""BotEnabled"": false,
  ""Prefix"": ""!"",
  ""Token"": ""abc123"",
  ""TriggerEnabled"": true,
  ""ColorChannelID"": 42
}");

        BotConfig config = new BotConfig();
        var colors = new ConcurrentDictionary<ulong, string>();
        var triggers = new ConcurrentDictionary<string, string>();
        var messages = new ConcurrentDictionary<ulong, LoggedMessage>();
        var ignore = new List<ulong>();
        var voidChannels = new ConcurrentDictionary<ulong, VoidChannelConfiguration>();
        var voidMessages = new ConcurrentDictionary<ulong, ScheduledVoidMessage>();

        BotStartup.Startup(ref config, ref colors, ref triggers, ref messages, ref ignore, ref voidChannels, ref voidMessages);

        Assert.Equal("!", config.Prefix);
        Assert.Equal("abc123", config.Token);
        Assert.True(config.TriggerEnabled);
        Assert.False(config.BotEnabled);
        Assert.Equal(42ul, config.ColorChannelID);
    }

    [Fact]
    public void Startup_WhenLegacyRootJsonExists_MigratesIntoCanonicalDataDirectory()
    {
        Directory.CreateDirectory(Path.Combine(_testRoot, "json"));
        File.WriteAllText(Path.Combine(_testRoot, "json", "BotConfig.json"),
@"{
  ""BotEnabled"": false,
  ""Prefix"": ""%"",
  ""Token"": ""legacy-root-token"",
  ""GeneralChannelID"": 77
}");

        BotConfig config = new BotConfig();
        var colors = new ConcurrentDictionary<ulong, string>();
        var triggers = new ConcurrentDictionary<string, string>();
        var messages = new ConcurrentDictionary<ulong, LoggedMessage>();
        var ignore = new List<ulong>();
        var voidChannels = new ConcurrentDictionary<ulong, VoidChannelConfiguration>();
        var voidMessages = new ConcurrentDictionary<ulong, ScheduledVoidMessage>();

        BotStartup.Startup(ref config, ref colors, ref triggers, ref messages, ref ignore, ref voidChannels, ref voidMessages);

        Assert.Equal("%", config.Prefix);
        Assert.Equal("legacy-root-token", config.Token);
        Assert.Equal(77ul, config.GeneralChannelID);
        Assert.True(File.Exists(BotStartup.BotConfigPath));
    }

    [Fact]
    public void SaveConfig_WritesConfigThatLoadConfigCanRead()
    {
        Directory.CreateDirectory(_dataRoot);
        BotConfig expected = new BotConfig
        {
            BotEnabled = false,
            Prefix = "!",
            Token = "persisted-token",
            AdminID = 999,
            ColorChannelID = 321,
            TriggerEnabled = true
        };

        BotStartup.SaveConfig(expected);
        BotConfig loaded = BotStartup.LoadConfig();

        Assert.False(loaded.BotEnabled);
        Assert.Equal("!", loaded.Prefix);
        Assert.Equal("persisted-token", loaded.Token);
        Assert.Equal(999ul, loaded.AdminID);
        Assert.Equal(321ul, loaded.ColorChannelID);
        Assert.True(loaded.TriggerEnabled);
    }

    [Fact]
    public void DataDirectoryPath_UsesOverrideDirectory()
    {
        Assert.Equal(Path.GetFullPath(_dataRoot), BotStartup.DataDirectoryPath);
    }

    [Fact]
    public void LoadConfig_WhenEnvironmentTokenIsSet_UsesEnvironmentValue()
    {
        Directory.CreateDirectory(_dataRoot);
        File.WriteAllText(BotStartup.BotConfigPath,
@"{
  ""Token"": ""file-token"",
  ""Prefix"": ""+""
}");
        Environment.SetEnvironmentVariable(BotStartup.TokenEnvironmentVariableName, "env-token");

        BotConfig loaded = BotStartup.LoadConfig();

        Assert.Equal("env-token", loaded.Token);
    }

    [Fact]
    public void SaveConfig_WhenEnvironmentTokenIsSet_DoesNotPersistTokenToDisk()
    {
        Directory.CreateDirectory(_dataRoot);
        Environment.SetEnvironmentVariable(BotStartup.TokenEnvironmentVariableName, "env-token");
        BotConfig config = new BotConfig
        {
            Token = "env-token",
            Prefix = "!",
            AdminID = 42
        };

        BotStartup.SaveConfig(config);

        string rawJson = File.ReadAllText(BotStartup.BotConfigPath);
        Assert.DoesNotContain("env-token", rawJson);
        BotConfig persisted = Newtonsoft.Json.JsonConvert.DeserializeObject<BotConfig>(rawJson)!;
        Assert.Equal(42ul, persisted.AdminID);
        Assert.Null(persisted.Token);

        BotConfig loaded = BotStartup.LoadConfig();
        Assert.Equal("env-token", loaded.Token);
        Assert.Equal(42ul, loaded.AdminID);
    }

    [Fact]
    public void BotConfig_DefaultsBotEnabledToTrue()
    {
        BotConfig config = new BotConfig();

        Assert.True(config.BotEnabled);
    }

    public void Dispose()
    {
        BotStartup.ClearDataDirectoryOverride();
        Environment.SetEnvironmentVariable(BotStartup.TokenEnvironmentVariableName, _originalTokenEnvironmentValue);
        Directory.SetCurrentDirectory(_originalDirectory);
        try
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }
        }
        catch
        {
            // ignore cleanup failures
        }
    }
}
