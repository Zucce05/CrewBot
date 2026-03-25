using CrewBot.Classes;

namespace CrewBot.Tests;

public class BotRuntimeDefaultsTests
{
    [Fact]
    public void MessageCacheCleanupInterval_IsOneHourMilliseconds()
    {
        Assert.Equal(3_600_000d, BotRuntimeDefaults.MessageCacheCleanupIntervalMs);
    }

    [Fact]
    public void ColorSelectionInterval_IsConfiguredConstant()
    {
        Assert.Equal(100_000d, BotRuntimeDefaults.ColorSelectionIntervalMs);
    }

    [Fact]
    public void ConfigRefreshInterval_IsFiveSecondsMilliseconds()
    {
        Assert.Equal(5_000d, BotRuntimeDefaults.ConfigRefreshIntervalMs);
    }

    [Fact]
    public void MessageCacheFlushInterval_IsFiveSecondsMilliseconds()
    {
        Assert.Equal(5_000d, BotRuntimeDefaults.MessageCacheFlushIntervalMs);
    }
}
