using CrewBot.Classes;
using Discord;
using System;
using System.IO;

namespace CrewBot.Tests;

public class LoggingTests : IDisposable
{
    private readonly string _testRoot;

    public LoggingTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "CrewBotLoggingTests", Guid.NewGuid().ToString("N"));
        BotStartup.SetDataDirectoryOverride(Path.Combine(_testRoot, "CrewBot", "json"));
    }

    [Fact]
    public async Task Log_WritesSeverityFileUnderCanonicalLogsDirectory()
    {
        var logging = new Logging();
        var message = new LogMessage(LogSeverity.Info, "Tests", "bot startup test message");

        await logging.Log(message);

        string infoLogPath = Path.Combine(BotStartup.LogsDirectoryPath, "InfoLog.txt");
        Assert.True(File.Exists(infoLogPath));
        string logContents = File.ReadAllText(infoLogPath);
        Assert.Contains("bot startup test message", logContents);
    }

    [Fact]
    public void Constructor_CreatesExpectedLogFilesUnderCanonicalLogsDirectory()
    {
        _ = new Logging();

        Assert.True(File.Exists(Path.Combine(BotStartup.LogsDirectoryPath, "CriticalLog.txt")));
        Assert.True(File.Exists(Path.Combine(BotStartup.LogsDirectoryPath, "ErrorLog.txt")));
        Assert.True(File.Exists(Path.Combine(BotStartup.LogsDirectoryPath, "WarningLog.txt")));
        Assert.True(File.Exists(Path.Combine(BotStartup.LogsDirectoryPath, "InfoLog.txt")));
        Assert.True(File.Exists(Path.Combine(BotStartup.LogsDirectoryPath, "VerboseLog.txt")));
        Assert.True(File.Exists(Path.Combine(BotStartup.LogsDirectoryPath, "DebugLog.txt")));
    }

    public void Dispose()
    {
        BotStartup.ClearDataDirectoryOverride();
        try
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }
        }
        catch
        {
            // Ignore temp cleanup failures in tests.
        }
    }
}
